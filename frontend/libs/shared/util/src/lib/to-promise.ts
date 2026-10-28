import { firstValueFrom, fromEvent, mergeMap, race, throwError, type Observable } from 'rxjs'

export function toPromise<T>(source: Observable<T>, signal?: AbortSignal): Promise<T> {
	if (!signal) {
		return firstValueFrom(source)
	}

	if (signal.aborted) {
		return Promise.reject(signal.reason)
	}

	return firstValueFrom(
		race(source, fromEvent(signal, 'abort').pipe(mergeMap(() => throwError(() => signal.reason)))),
	)
}
