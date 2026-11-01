import { DestroyRef, ErrorHandler, InjectionToken, inject, type Provider } from '@angular/core'
import { ObservableHub } from './observable-hub'
import { SIGNALR_CONFIG, type SignalRConfig } from './provide-signalr'

/** Install in the owning component injector so its DestroyRef also owns the connection. */
export function provideHub<Methods, Events>(
	token: InjectionToken<ObservableHub<Methods, Events>>,
	url: string | (() => string),
	config: SignalRConfig = {},
): Provider {
	return {
		provide: token,
		useFactory: () => {
			const hub = new ObservableHub<Methods, Events>(typeof url === 'function' ? url() : url, {
				...inject(SIGNALR_CONFIG),
				...config,
			})
			const errors = inject(ErrorHandler)

			inject(DestroyRef).onDestroy(() => {
				hub.destroy().catch(error => errors.handleError(error))
			})

			return hub
		},
	}
}
