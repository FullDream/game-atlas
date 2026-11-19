import { Location } from '@angular/common'
import { inject } from '@angular/core'
import { Router } from '@angular/router'
import { QueryClient } from '@tanstack/angular-query'

export const adminDestination = (url: string | null): string => {
	if (!url?.startsWith('/') || url.startsWith('//') || url.includes('\\')) return '/games'

	const destination = new URL(url, window.location.origin)

	if (destination.origin !== window.location.origin || !/^\/(games|media)(\/|$)/.test(destination.pathname))
		return '/games'

	return destination.pathname + destination.search + destination.hash
}

let redirecting = false

export const injectLoginRedirect = () => {
	const router = inject(Router)
	const location = inject(Location)
	const client = inject(QueryClient)

	return (returnUrl: string) => {
		if (redirecting) return

		const tree = router.createUrlTree(['/auth/login'], { queryParams: { returnUrl: adminDestination(returnUrl) } })
		const target = location.prepareExternalUrl(router.serializeUrl(tree))

		client.clear()
		window.location.replace(target)
		redirecting = true
	}
}
