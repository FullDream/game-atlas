import { HttpErrorResponse, HttpStatusCode, type HttpInterceptorFn } from '@angular/common/http'
import { inject } from '@angular/core'
import { Router } from '@angular/router'
import { catchError, throwError } from 'rxjs'
import { injectLoginRedirect } from './login-redirect'

export const authInterceptor: HttpInterceptorFn = (req, next) => {
	const redirect = injectLoginRedirect()
	const router = inject(Router)

	return next(req).pipe(
		catchError((error: unknown) => {
			if (
				error instanceof HttpErrorResponse &&
				error.status === HttpStatusCode.Unauthorized &&
				req.url.startsWith('/api/') &&
				!req.url.startsWith('/api/auth/') &&
				!router.url.startsWith('/auth/login')
			) {
				redirect(router.url)
			}

			return throwError(() => error)
		}),
	)
}
