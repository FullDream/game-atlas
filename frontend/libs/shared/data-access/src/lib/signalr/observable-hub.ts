import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'
import { BehaviorSubject, Observable, defer, firstValueFrom, from, map } from 'rxjs'
import type { SignalRConfig } from './provide-signalr'

export type HubState = { status: 'closed' | 'connecting' | 'connected' | 'reconnecting'; error?: unknown }
type Arguments<T> = T extends unknown[] ? T : never

/** One independently owned connection; observing events never starts another connection. */
export class ObservableHub<Methods, Events> {
	readonly #connection
	readonly #state = new BehaviorSubject<HubState>({ status: 'closed' })
	readonly state$ = this.#state.asObservable()
	#transition: Promise<unknown> = Promise.resolve()
	#destroyed = false

	constructor(url: string, config: SignalRConfig) {
		this.#connection = new HubConnectionBuilder()
			.withUrl(url, config.connectionOptions ?? {})
			.withAutomaticReconnect(config.retryDelays ?? [0, 2000, 10000, 30000])
			.configureLogging(LogLevel.Warning)
			.build()
		this.#connection.onreconnecting(error => this.#state.next({ status: 'reconnecting', error }))
		this.#connection.onreconnected(() => this.#state.next({ status: 'connected' }))
		this.#connection.onclose(error => this.#state.next({ status: 'closed', error }))
	}

	connect(): Observable<HubState> {
		return defer(() =>
			this.#serialize(async () => {
				if (this.#destroyed) throw new Error('Hub owner has been destroyed')

				if (this.#connection.state === HubConnectionState.Connected) return this.#state.value

				this.#state.next({ status: 'connecting' })
				try {
					await this.#connection.start()
					if (this.#destroyed) {
						await this.#connection.stop()
						throw new Error('Hub owner has been destroyed')
					}

					const state: HubState = { status: 'connected' }

					this.#state.next(state)

					return state
				} catch (error) {
					this.#state.next({ status: 'closed', error })
					throw error
				}
			}),
		)
	}

	disconnect(beforeStop?: Observable<unknown>): Observable<HubState> {
		return defer(() =>
			this.#serialize(async () => {
				try {
					if (beforeStop && this.#connection.state === HubConnectionState.Connected)
						await firstValueFrom(beforeStop)
				} finally {
					await this.#connection.stop()
				}
			}),
		).pipe(map(() => ({ status: 'closed' })))
	}

	invoke<Key extends keyof Methods & string>(method: Key, ...args: Arguments<Methods[Key]>): Observable<unknown> {
		return defer(() => from(this.#connection.invoke<unknown>(method, ...args)))
	}

	on<Key extends keyof Events & string>(event: Key): Observable<Arguments<Events[Key]>> {
		return new Observable(subscriber => {
			const handler = (...args: Arguments<Events[Key]>): void => subscriber.next(args)

			this.#connection.on(event, handler)

			return () => this.#connection.off(event, handler)
		})
	}

	/** Called by provideHub's DestroyRef, including while start is in flight. */
	destroy(): Promise<HubState> {
		this.#destroyed = true

		return this.#connection.stop().then(() => ({ status: 'closed' }))
	}

	#serialize<Result>(operation: () => Promise<Result>): Promise<Result> {
		const result = this.#transition.then(operation, operation)

		this.#transition = result

		return result
	}
}
