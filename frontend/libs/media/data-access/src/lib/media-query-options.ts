import { HttpErrorResponse, HttpEventType } from '@angular/common/http'
import { inject, Service, signal } from '@angular/core'
import { toPromise } from '@game-atlas/shared/util'
import {
	hashKey,
	keepPreviousData,
	QueryClient,
	queryOptions,
	type CreateMutationOptions,
	type CreateQueryOptions,
	type Query,
	type QueryKey,
} from '@tanstack/angular-query-experimental'
import { omit, uniqueBy } from 'remeda'
import { filter, map, tap } from 'rxjs'
import { MediaService } from './generated/media/media.service'
import type {
	CreateMediaFolderRequest,
	ListMediaParams,
	MediaAsset,
	MediaFolderDetails,
	MediaFolder,
	MediaItem,
	PaginatedResultOfMediaItem,
	UpdateMediaAssetRequest,
	UpdateMediaFolderRequest,
} from './generated/models'
import type { MediaUploadStateOptions, UploadMediaVariables } from './upload-media-variables'
import { mediaParent, reconcileMediaFolder, reconcileMediaList } from './utils'

type CachedMedia = PaginatedResultOfMediaItem | MediaFolderDetails

type MediaCacheSnapshot = {
	key: QueryKey
	previous: CachedMedia
	optimistic: CachedMedia
	refresh: boolean
}

type OptimisticMediaContext = {
	next: MediaItem | null
	snapshots: MediaCacheSnapshot[]
}

export function mediaListParams(params: ListMediaParams = {}): ListMediaParams {
	return {
		folderId: params.folderId || undefined,
		offset: params.offset ?? 0,
		limit: params.limit ?? 24,
		search: params.search?.trim() || undefined,
	}
}

@Service()
export class MediaQueryOptions {
	readonly #mediaService = inject(MediaService)
	readonly #queryClient = inject(QueryClient)

	readonly #uploadProgress = signal<Record<string, number | null>>({})

	readonly uploadKey = ['media', 'upload'] as const
	readonly uploadProgress = this.#uploadProgress.asReadonly()

	list(params: ListMediaParams = {}): CreateQueryOptions<PaginatedResultOfMediaItem, HttpErrorResponse> {
		const normalized = mediaListParams(params)

		return queryOptions<PaginatedResultOfMediaItem, HttpErrorResponse>({
			queryKey: ['media', 'list', normalized],
			staleTime: 30_000,
			queryFn: ({ signal }) => toPromise(this.#mediaService.listMedia(normalized), signal),
			placeholderData: (previousData, previousQuery) => {
				const previousParams = previousQuery?.queryKey[2] as ListMediaParams | undefined

				if (previousParams?.folderId !== normalized.folderId) return undefined

				return keepPreviousData(previousData)
			},
		})
	}

	folder(id: string | null): CreateQueryOptions<MediaFolderDetails, HttpErrorResponse> {
		return queryOptions<MediaFolderDetails, HttpErrorResponse>({
			queryKey: ['media', 'folder', id],
			enabled: !!id,
			staleTime: 30_000,
			queryFn: ({ signal }) => {
				if (!id) return Promise.reject(new Error('A folder ID is required.'))

				return toPromise(this.#mediaService.getMediaFolder(id), signal)
			},
		})
	}

	getCachedFolder(id: string | null): MediaFolder | undefined {
		if (!id) return undefined

		let latestFolder: MediaFolder | undefined
		let updatedAt = -1

		for (const query of this.#queryClient.getQueryCache().findAll({ queryKey: ['media'] })) {
			const data = query.state.data as CachedMedia | undefined
			let folder: MediaFolder | undefined

			if (query.queryKey[1] === 'folder' && data && 'folder' in data && data.folder.id === id) {
				folder = data.folder
			} else if (query.queryKey[1] === 'list' && data && 'items' in data) {
				const item = data.items.find(item => item.id === id)

				if (item?.type === 'folder') folder = item
			}

			if (folder && query.state.dataUpdatedAt > updatedAt) {
				latestFolder = folder
				updatedAt = query.state.dataUpdatedAt
			}
		}

		return latestFolder
	}

	createFolder(): CreateMutationOptions<MediaFolder, HttpErrorResponse, CreateMediaFolderRequest> {
		return {
			mutationKey: ['media', 'create-folder'],
			mutationFn: request => toPromise(this.#mediaService.createMediaFolder(request)),
			onSuccess: folder => this.#commit(null, folder),
		}
	}

	updateFolder(): CreateMutationOptions<
		MediaFolder,
		HttpErrorResponse,
		{ id: string; patch: UpdateMediaFolderRequest },
		OptimisticMediaContext | undefined
	> {
		return {
			mutationKey: ['media', 'update-folder'],
			onMutate: ({ id, patch }) => {
				const previous = this.#findCachedItem(id)

				if (previous?.type !== 'folder') return

				return this.#updateCache(previous, {
					...previous,
					name: patch.name?.trim() ?? previous.name,
					parentId: patch.parentId === undefined ? previous.parentId : patch.parentId,
				})
			},
			mutationFn: ({ id, patch }) => toPromise(this.#mediaService.updateMediaFolder(id, patch)),
			onSuccess: (folder, variables, context) =>
				context
					? this.#commit(context.next, folder, context)
					: this.#queryClient.invalidateQueries({ queryKey: ['media'] }),
			onError: (error, variables, context) => this.#rollback(context),
		}
	}

	updateAsset(): CreateMutationOptions<
		MediaAsset,
		HttpErrorResponse,
		{ id: string; patch: UpdateMediaAssetRequest },
		OptimisticMediaContext | undefined
	> {
		return {
			mutationKey: ['media', 'update-asset'],
			onMutate: ({ id, patch }) => {
				const previous = this.#findCachedItem(id)

				if (!previous || previous.type === 'folder') return

				return this.#updateCache(previous, {
					...previous,
					name: patch.name?.trim() ?? previous.name,
					folderId: patch.folderId === undefined ? previous.folderId : patch.folderId,
					...(previous.type === 'image' ? { alt: patch.alt ?? previous.alt } : {}),
				})
			},
			mutationFn: ({ id, patch }) => toPromise(this.#mediaService.updateMediaAsset(id, patch)),
			onSuccess: (asset, variables, context) =>
				context
					? this.#commit(context.next, asset, context)
					: this.#queryClient.invalidateQueries({ queryKey: ['media'] }),
			onError: (error, variables, context) => this.#rollback(context),
		}
	}

	remove(): CreateMutationOptions<void, HttpErrorResponse, MediaItem> {
		return {
			mutationKey: ['media', 'delete'],
			mutationFn: item =>
				toPromise(
					item.type === 'folder'
						? this.#mediaService.deleteMediaFolder(item.id)
						: this.#mediaService.deleteMediaAsset(item.id),
				),
			onSuccess: (data, item) => this.#commit(item, null),
		}
	}

	upload(): CreateMutationOptions<MediaAsset, HttpErrorResponse, UploadMediaVariables> {
		return {
			mutationKey: this.uploadKey,
			retry: false,
			onMutate: ({ key }) => {
				const cache = this.#queryClient.getMutationCache()

				cache
					.findAll({
						mutationKey: this.uploadKey,
						status: 'error',
						predicate: mutation =>
							(mutation.state.variables as UploadMediaVariables | undefined)?.key === key,
					})
					.forEach(mutation => cache.remove(mutation))

				this.#uploadProgress.update(progress => ({ ...progress, [key]: 0 }))
			},
			mutationFn: ({ key, file, folderId, controller }) =>
				toPromise(
					this.#mediaService
						.uploadMediaAsset({ file, folderId }, { observe: 'events', reportProgress: true })
						.pipe(
							tap(event => {
								if (event.type !== HttpEventType.UploadProgress) return

								const percent = event.total
									? Math.min(100, Math.round((event.loaded / event.total) * 100))
									: null

								this.#uploadProgress.update(progress => ({ ...progress, [key]: percent }))
							}),
							filter(event => event.type === HttpEventType.Response),
							map(event => {
								if (!event.body) throw new Error('Upload returned no asset')

								return event.body
							}),
						),
					controller.signal,
				),
			onSuccess: asset => this.#commit(null, asset),
			onSettled: (asset, error, { key }) => this.#uploadProgress.update(progress => omit(progress, [key])),
		}
	}

	uploadState(folderId: string | null): MediaUploadStateOptions {
		return {
			filters: {
				mutationKey: this.uploadKey,
				predicate: mutation => {
					const variables = mutation.state.variables as UploadMediaVariables | undefined

					return (
						!!variables &&
						variables.folderId === folderId &&
						!variables.controller.signal.aborted &&
						(mutation.state.status === 'pending' || mutation.state.status === 'error')
					)
				},
			},
			select: mutation => ({
				remove: () => this.#queryClient.getMutationCache().remove(mutation),
				submittedAt: mutation.state.submittedAt,
				variables: mutation.state.variables as UploadMediaVariables,
				status: mutation.state.status,
				error: mutation.state.error,
			}),
		}
	}

	#findCachedItem(id: string): MediaItem | undefined {
		const folder = this.#queryClient.getQueryData<MediaFolderDetails>(['media', 'folder', id])?.folder

		if (folder) return folder

		for (const [, data] of this.#queryClient.getQueriesData<PaginatedResultOfMediaItem>({
			queryKey: ['media', 'list'],
		})) {
			const item = data?.items.find(item => item.id === id)
			const video = data?.items.find(item => item.type === 'video' && item.preview?.id === id)

			if (item) return item

			if (video?.type === 'video' && video.preview) return video.preview
		}

		return undefined
	}

	#affectedQueries(previous: MediaItem | null, next: MediaItem | null): Query[] {
		const entity = next ?? previous

		if (!entity) return []

		const parents = [previous ? mediaParent(previous) : undefined, next ? mediaParent(next) : undefined]

		return this.#queryClient.getQueryCache().findAll({
			queryKey: ['media'],
			predicate: query => {
				const data = query.state.data as CachedMedia | undefined

				if (!data) return false

				if (query.queryKey[1] === 'folder' && 'folder' in data) {
					return (
						parents.includes(data.folder.id) ||
						data.folder.id === entity.id ||
						data.ancestors.some(parent => parent.id === entity.id)
					)
				}

				if (query.queryKey[1] !== 'list' || !('items' in data)) return false

				const params = query.queryKey[2] as ListMediaParams

				return (
					parents.includes(params.folderId ?? null) ||
					data.items.some(
						item =>
							item.id === entity.id ||
							(item.type === 'folder' && parents.includes(item.id)) ||
							(item.type === 'video' && item.preview?.id === entity.id),
					)
				)
			},
		})
	}

	async #updateCache(previous: MediaItem | null, next: MediaItem | null): Promise<OptimisticMediaContext> {
		const queries = this.#affectedQueries(previous, next)

		await Promise.all(
			queries.map(query => this.#queryClient.cancelQueries({ queryKey: query.queryKey, exact: true })),
		)

		const snapshots = queries.flatMap((query): MediaCacheSnapshot[] => {
			const data = query.state.data as CachedMedia | undefined

			if (!data) return []

			const result =
				'folder' in data
					? reconcileMediaFolder(data, previous, next)
					: reconcileMediaList(data, query.queryKey[2] as ListMediaParams, previous, next)
			const optimistic = this.#queryClient.setQueryData<CachedMedia>(query.queryKey, result.data)

			return optimistic ? [{ key: query.queryKey, previous: data, optimistic, refresh: result.invalidate }] : []
		})

		return { next, snapshots }
	}

	async #commit(previous: MediaItem | null, next: MediaItem | null, context?: OptimisticMediaContext): Promise<void> {
		const pending =
			context?.snapshots.filter(
				snapshot => snapshot.refresh || this.#queryClient.getQueryData(snapshot.key) !== snapshot.optimistic,
			) ?? []
		const confirmed = await this.#updateCache(context ? context.next : previous, next)
		const refresh = uniqueBy([...pending, ...confirmed.snapshots.filter(snapshot => snapshot.refresh)], snapshot =>
			hashKey(snapshot.key),
		)

		await Promise.all(
			refresh.map(snapshot => this.#queryClient.invalidateQueries({ queryKey: snapshot.key, exact: true })),
		)
	}

	async #rollback(context: OptimisticMediaContext | undefined): Promise<void> {
		await Promise.all(
			(context?.snapshots ?? []).map(async snapshot => {
				await this.#queryClient.cancelQueries({ queryKey: snapshot.key, exact: true })

				if (this.#queryClient.getQueryData(snapshot.key) !== snapshot.optimistic) {
					await this.#queryClient.invalidateQueries({ queryKey: snapshot.key, exact: true })

					return
				}

				this.#queryClient.setQueryData(snapshot.key, snapshot.previous)
			}),
		)
	}
}
