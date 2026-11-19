import { HttpErrorResponse } from '@angular/common/http'
import { Component, computed, inject, signal } from '@angular/core'
import { disabled, form, FormField, FormRoot, required } from '@angular/forms/signals'
import { ActivatedRoute, Router } from '@angular/router'
import { AuthQueryOptions, adminDestination } from '@game-atlas/auth/data-access'
import { Button, Field, getErrorMessageFromFieldState } from '@game-atlas/shared/ui'
import { HlmCard } from '@spartan-ng/helm/card'
import { HlmFieldError } from '@spartan-ng/helm/field'
import { HlmInput } from '@spartan-ng/helm/input'
import { injectMutation } from '@tanstack/angular-query'

@Component({
	selector: 'lib-feature-login',
	imports: [Button, Field, FormField, FormRoot, HlmCard, HlmFieldError, HlmInput],
	templateUrl: './feature-login.html',
})
export class FeatureLogin {
	readonly #auth = inject(AuthQueryOptions)
	readonly #router = inject(Router)
	readonly #params = inject(ActivatedRoute).snapshot.queryParamMap

	readonly #signIn = injectMutation(() => ({
		...this.#auth.login(() => this.#credentials()),
		onSuccess: async () => {
			await this.#router.navigateByUrl(adminDestination(this.#params.get('returnUrl')), { replaceUrl: true })
		},
	}))

	readonly #credentials = signal({ login: '', password: '' })

	protected readonly fields = form(
		this.#credentials,
		path => {
			required(path.login, { message: 'Enter your username.' })
			required(path.password, { message: 'Enter your password.' })
			disabled(path, { when: () => this.#signIn.isPending() })
		},
		{
			submission: {
				ignoreValidators: 'all',
				action: async fields => {
					if (!fields.login().valid() || !fields.password().valid()) return undefined

					try {
						await this.#signIn.mutateAsync()

						return undefined
					} catch (error) {
						const problem = error instanceof HttpErrorResponse ? error.error : undefined
						const message = [problem?.detail, problem?.title].find(
							(value): value is string => typeof value === 'string' && !!value.trim(),
						)

						return { kind: 'server', message: message ?? 'Unable to sign in. Try again.' }
					}
				},
			},
		},
	)

	protected readonly message = computed(() => getErrorMessageFromFieldState(this.fields()))
	protected readonly unavailable = computed(() => this.#signIn.isIdle() && this.#params.has('unavailable'))
}
