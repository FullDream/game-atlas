import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http'
import { ErrorHandler, Service, inject } from '@angular/core'
import { QueryClient, isCancelledError } from '@tanstack/angular-query-experimental'
import { toPromise } from '@game-atlas/shared/util'
import {
	Observable,
	auditTime,
	catchError,
	concat,
	defer,
	distinctUntilChanged,
	filter,
	finalize,
	map,
	merge,
	of,
	share,
	startWith,
	switchMap,
	takeWhile,
	timeout,
} from 'rxjs'
import { MAP_HUB, type MapProcessingUpdate } from './map-hub'
import { acceptMap, acceptStyle, withStyle } from './map-cache'
import { mapQueryKeys } from './map-query-keys'
import { MapsQueryOptions } from './maps-query-options'
import { MapsService } from './generated/maps/maps.service'
import type { Map as GameMap, MapStyle, UpdateMapStyleRequest } from './generated/models'

export type MapLiveState = { status: 'synchronizing' | 'live' | 'stale' | 'reconnecting' | 'deleted'; error?: unknown }
type Session = {
	gameId: string
	mapId: string
	deleted: boolean
	removed: Set<string>
	attempts: Map<string, string | null>
	retired: Set<string>
	needsRead: boolean
}

@Service()
export class MapsLive {
	readonly #hub = inject(MAP_HUB)
	readonly #options = inject(MapsQueryOptions)
	readonly #api = inject(MapsService)
	readonly #client = inject(QueryClient)
	readonly #errors = inject(ErrorHandler)

	/** One subscription owns connect -> join -> snapshot/events -> leave. */
	watch(gameId: string, mapId: string): Observable<MapLiveState> {
		return defer(() => {
			const session: Session = {
				gameId,
				mapId,
				deleted: false,
				removed: new Set(),
				attempts: new Map(),
				retired: new Set(),
				needsRead: false,
			}

			return this.#hub.connect().pipe(
				switchMap(() =>
					this.#hub.state$.pipe(
						distinctUntilChanged((a, b) => a.status === b.status),
						switchMap(state =>
							state.status === 'connected'
								? this.#connected(session)
								: of<MapLiveState>({
										status: state.status === 'reconnecting' ? 'reconnecting' : 'stale',
										error: state.error,
									}),
						),
					),
				),
				catchError(error =>
					this.#snapshot(session).pipe(
						map(state => (state.status === 'live' ? ({ status: 'stale', error } as MapLiveState) : state)),
					),
				),
				takeWhile(state => state.status !== 'deleted', true),
				startWith({ status: 'synchronizing' } as MapLiveState),
				finalize(() => {
					this.#client
						.cancelQueries({ queryKey: mapQueryKeys.detail(gameId, mapId), exact: true })
						.catch(error => this.#errors.handleError(error))
					this.#hub
						.disconnect(this.#hub.invoke('LeaveMap', mapId).pipe(timeout(5000)))
						.subscribe({ error: error => this.#errors.handleError(error) })
				}),
			)
		})
	}

	#connected(session: Session): Observable<MapLiveState> {
		const events$ = merge(
			this.#hub.on('StyleUpdated').pipe(map(([style]) => this.#styleChanged(session, style))),
			this.#hub.on('ProcessingUpdated').pipe(
				filter(([update]) => update.mapId === session.mapId),
				map(([update]) => this.#processingChanged(session, update)),
			),
		).pipe(
			map(read => {
				session.needsRead ||= read

				return read
			}),
			share(),
		)
		const mutations$ = new Observable<boolean>(subscriber =>
			this.#client.getMutationCache().subscribe(event => {
				const mutation = event.mutation

				if (event.type !== 'updated' || mutation?.options.scope?.id !== `map:${session.mapId}`) return

				if (mutation.options.mutationKey?.[2] === 'retry-style') {
					const { styleId } = mutation.state.variables as { styleId: string }
					const attempt = session.attempts.get(styleId)

					if (attempt) session.retired.add(attempt)

					session.attempts.delete(styleId)
					session.needsRead = true
				}

				if (mutation.state.status !== 'pending') subscriber.next(session.needsRead)
			}),
		)
		const requests$ = concat(
			this.#hub.invoke('JoinMap', session.mapId).pipe(map(() => true)),
			merge(events$, mutations$).pipe(filter(Boolean)),
		).pipe(
			auditTime(0),
			filter(
				() =>
					!session.deleted &&
					this.#client.isMutating({
						predicate: mutation => mutation.options.scope?.id === `map:${session.mapId}`,
					}) === 0,
			),
			switchMap(() => this.#snapshot(session)),
		)

		// Register handlers before JoinMap. The first snapshot covers events received during the join.
		return merge(
			events$.pipe(
				filter(() => session.deleted),
				map((): MapLiveState => ({ status: 'deleted' })),
			),
			requests$,
		)
	}

	#snapshot(session: Session): Observable<MapLiveState> {
		return defer(() => {
			session.needsRead = false

			return this.#client.fetchQuery({
				...this.#options.getDetail(session.gameId, session.mapId),
				staleTime: 0,
				queryFn: async ({ signal }) => {
					const response = await toPromise(this.#api.getMap(session.gameId, session.mapId), signal)

					if (signal.aborted) throw signal.reason

					if (session.deleted) throw new HttpErrorResponse({ status: HttpStatusCode.NotFound })

					const merged = acceptMap(
						this.#client.getQueryData<GameMap>(mapQueryKeys.detail(session.gameId, session.mapId)),
						response,
					)

					const result = { ...merged, styles: merged.styles.filter(style => !session.removed.has(style.id)) }

					this.#client.setQueryData<GameMap[]>(mapQueryKeys.list(session.gameId), maps =>
						maps?.map(item => (item.id === session.mapId ? result : item)),
					)

					return result
				},
			})
		}).pipe(
			map((): MapLiveState => ({ status: 'live' })),
			catchError(error => {
				session.needsRead = isCancelledError(error)

				return of<MapLiveState>({
					status:
						error instanceof HttpErrorResponse && error.status === HttpStatusCode.NotFound
							? 'deleted'
							: 'stale',
					error,
				})
			}),
			startWith({ status: 'synchronizing' } as MapLiveState),
			finalize(() => {
				this.#client
					.cancelQueries({ queryKey: mapQueryKeys.detail(session.gameId, session.mapId), exact: true })
					.catch(error => this.#errors.handleError(error))
			}),
		)
	}

	#styleChanged(session: Session, incoming: MapStyle): boolean {
		if (session.deleted || session.removed.has(incoming.id)) return false

		const pending = this.#client.getMutationCache().findAll({
			status: 'pending',
			predicate: mutation => mutation.options.scope?.id === `map:${session.mapId}`,
		})
		const editing = pending.find(
			mutation => (mutation.state.variables as { styleId?: string }).styleId === incoming.id,
		)
		const patch = (editing?.state.variables as { patch?: UpdateMapStyleRequest } | undefined)?.patch
		const previous = this.#client
			.getQueryData<GameMap>(mapQueryKeys.detail(session.gameId, session.mapId))
			?.styles.find(style => style.id === incoming.id)
		const style = { ...acceptStyle(previous, incoming), ...patch }

		if (editing?.options.mutationKey?.[2] === 'retry-style') return true

		this.#updateCache(session, current =>
			current.styles.some(item => item.id === style.id)
				? withStyle(current, style.id, () => style)
				: { ...current, styles: [...current.styles, style] },
		)

		return !previous || previous.status !== style.status
	}

	#processingChanged(session: Session, update: MapProcessingUpdate): boolean {
		if (update.deleted) {
			const styleId = update.styleId

			if (styleId) {
				session.removed.add(styleId)
				this.#updateCache(session, current => withStyle(current, styleId, () => undefined))

				return true
			}

			session.deleted = true
			this.#client.removeQueries({ queryKey: mapQueryKeys.detail(session.gameId, session.mapId), exact: true })
			this.#client.setQueryData<GameMap[]>(mapQueryKeys.list(session.gameId), maps =>
				maps?.filter(item => item.id !== session.mapId),
			)

			return false
		}

		const { styleId, attemptId } = update
		const style = this.#client
			.getQueryData<GameMap>(mapQueryKeys.detail(session.gameId, session.mapId))
			?.styles.find(item => item.id === styleId)

		if (
			!styleId ||
			!attemptId ||
			update.status !== 'processing' ||
			session.removed.has(styleId) ||
			session.retired.has(attemptId) ||
			style?.status !== 'processing'
		)
			return false

		const previous = session.attempts.get(styleId)

		if (!session.attempts.has(styleId)) {
			session.attempts.set(styleId, attemptId)

			return true
		}

		if (previous !== attemptId) {
			session.retired.add(attemptId)
			if (previous) session.retired.add(previous)

			session.attempts.set(styleId, null)

			return previous !== null
		}

		return this.#styleChanged(session, {
			...style,
			progress: Math.min(99, Math.max(style.progress, update.progress)),
		})
	}

	#updateCache(session: Session, change: (map: GameMap) => GameMap): GameMap | undefined {
		const result = this.#client.setQueryData<GameMap>(
			mapQueryKeys.detail(session.gameId, session.mapId),
			current => (current ? change(current) : current),
		)

		this.#client.setQueryData<GameMap[]>(mapQueryKeys.list(session.gameId), maps =>
			maps?.map(current => (current.id === session.mapId ? change(current) : current)),
		)

		return result
	}
}
