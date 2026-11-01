import { InjectionToken, type Provider } from '@angular/core'
import type { IHttpConnectionOptions } from '@microsoft/signalr'

export type SignalRConfig = {
	connectionOptions?: IHttpConnectionOptions
	retryDelays?: number[]
}

export const SIGNALR_CONFIG = new InjectionToken<SignalRConfig>('SignalR defaults', { factory: () => ({}) })

export function provideSignalR(config: SignalRConfig = {}): Provider {
	return { provide: SIGNALR_CONFIG, useValue: config }
}
