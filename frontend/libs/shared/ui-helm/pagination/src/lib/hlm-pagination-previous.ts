import type { BooleanInput } from '@angular/cdk/coercion'
import { booleanAttribute, Component, computed, input } from '@angular/core'
import type { RouterLink } from '@angular/router'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matChevronLeftRound } from '@ng-icons/material-symbols/round'
import type { ButtonVariants } from '@spartan-ng/helm/button'
import { hlm } from '@spartan-ng/helm/utils'
import type { ClassValue } from 'clsx'
import { HlmPaginationLink } from './hlm-pagination-link'
import { injectHlmPaginationConfig } from './hlm-pagination.token'

@Component({
	selector: 'hlm-pagination-previous',
	exportAs: 'hlmPaginationPrevious',
	imports: [HlmPaginationLink, NgIcon],
	templateUrl: './hlm-pagination-previous.html',
	providers: [provideIcons({ matChevronLeftRound })],
})
export class HlmPaginationPrevious {
	readonly #config = injectHlmPaginationConfig()

	readonly userClass = input<ClassValue>('', { alias: 'class' })
	/** The link to navigate to the previous page. */
	readonly link = input<RouterLink['routerLink']>()
	/** The query parameters to pass to the previous page. */
	readonly queryParams = input<RouterLink['queryParams']>()
	/** How to handle query parameters when navigating to the previous page. */
	readonly queryParamsHandling = input<RouterLink['queryParamsHandling']>()

	/** The aria-label for the previous page link. */
	readonly ariaLabel = input<string>(this.#config.previousAriaLabel, { alias: 'aria-label' })
	/** The text to display for the previous page link. */
	readonly text = input<string>(this.#config.previousLabel)
	/** Whether the button should only display the icon. */
	readonly iconOnly = input<boolean, BooleanInput>(false, {
		transform: booleanAttribute,
	})
	protected readonly labelClass = computed(() => hlm(this.iconOnly() ? 'sr-only' : 'sm:block hidden'))

	protected readonly size = computed<ButtonVariants['size']>(() => (this.iconOnly() ? 'icon' : 'default'))

	protected readonly computedClass = computed(() => hlm(!this.iconOnly() && 'ps-2!', this.userClass()))
}
