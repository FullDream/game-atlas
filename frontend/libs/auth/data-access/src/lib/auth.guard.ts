import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http'
import { inject } from '@angular/core'
import { type CanMatchFn, Router } from '@angular/router'
import { QueryClient } from '@tanstack/angular-query'
import { AuthQueryOptions } from './auth-query-options'
import { adminDestination } from './login-redirect'

export const authGuard: CanMatchFn = async () => {
	const auth = inject(AuthQueryOptions)
	const client = inject(QueryClient)
	const router = inject(Router)

	const target = router.currentNavigation()?.initialUrl
	const returnUrl = target ? router.serializeUrl(target) : '/games'

	try {
		await client.query(auth.session())

		return true
	} catch (error) {
		const unavailable = !(error instanceof HttpErrorResponse && error.status === HttpStatusCode.Unauthorized)

		client.clear()

		return router.createUrlTree(['/auth/login'], {
			queryParams: { returnUrl: adminDestination(returnUrl), unavailable: unavailable || null },
		})
	}
}
