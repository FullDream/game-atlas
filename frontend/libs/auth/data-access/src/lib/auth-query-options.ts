import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http'
import { Injectable, inject } from '@angular/core'
import {
	type CreateMutationOptions,
	type CreateQueryOptions,
	mutationOptions,
	queryOptions,
} from '@tanstack/angular-query'
import { toPromise } from '@game-atlas/shared/util'
import { AuthService } from './generated/auth/auth.service'
import type { AdminLogin, AdminSession } from './generated/models'

@Injectable({ providedIn: 'root' })
export class AuthQueryOptions {
	readonly #api = inject(AuthService)

	session(): CreateQueryOptions<AdminSession, HttpErrorResponse> {
		return queryOptions<AdminSession, HttpErrorResponse>({
			queryKey: ['auth', 'session'],
			staleTime: 5 * 60_000,
			networkMode: 'always',
			retry: false,
			queryFn: async ({ signal }) => {
				const session = await toPromise(this.#api.getSession(), signal)

				await toPromise(this.#api.getAntiforgery(), signal)

				return session
			},
		})
	}

	login(credentials: () => AdminLogin): CreateMutationOptions<void, HttpErrorResponse, void> {
		return mutationOptions({
			networkMode: 'always',
			gcTime: 0,
			retry: false,
			mutationFn: async () => {
				const login = credentials()

				await toPromise(this.#api.getAntiforgery())
				await toPromise(this.#api.login(login))
			},
		})
	}

	logout(): CreateMutationOptions<void, HttpErrorResponse, void> {
		return mutationOptions({
			networkMode: 'always',
			retry: false,
			mutationFn: async () => {
				try {
					await toPromise(this.#api.logout())
				} catch (error) {
					if (!(error instanceof HttpErrorResponse && error.status === HttpStatusCode.Unauthorized))
						throw error
				}
			},
		})
	}
}
