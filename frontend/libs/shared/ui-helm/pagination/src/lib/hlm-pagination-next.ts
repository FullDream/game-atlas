import type { BooleanInput } from '@angular/cdk/coercion'
import { booleanAttribute, Component, computed, input } from '@angular/core'
import type { RouterLink } from '@angular/router'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matChevronRightRound } from '@ng-icons/material-symbols/round'
import type { ButtonVariants } from '@spartan-ng/helm/button'
import { hlm } from '@spartan-ng/helm/utils'
import type { ClassValue } from 'clsx'
import { HlmPaginationLink } from './hlm-pagination-link'
import { injectHlmPaginationConfig } from './hlm-pagination.token'

@Component({
	selector: 'hlm-pagination-next',
	exportAs: 'hlmPaginationNext',
	imports: [HlmPaginationLink, NgIcon],
	templateUrl: './hlm-pagination-next.html',
	providers: [provideIcons({ matChevronRightRound })],
})
export class HlmPaginationNext {
	readonly #config = injectHlmPaginationConfig()

	readonly userClass = input<ClassValue>('', { alias: 'class' })
	/** The link to navigate to the next page. */
	readonly link = input<RouterLink['routerLink']>()
	/** The query parameters to pass to the next page. */
	readonly queryParams = input<RouterLink['queryParams']>()
	/** How to handle query parameters when navigating to the next page. */
	readonly queryParamsHandling = input<RouterLink['queryParamsHandling']>()

	/** The aria-label for the next page link. */
	readonly ariaLabel = input<string>(this.#config.nextAriaLabel, { alias: 'aria-label' })
	/** The text to display for the next page link. */
	readonly text = input<string>(this.#config.nextLabel)
	/** Whether the button should only display the icon. */
	readonly iconOnly = input<boolean, BooleanInput>(false, {
		transform: booleanAttribute,
	})
	protected readonly labelClass = computed(() => (this.iconOnly() ? 'sr-only' : 'hidden sm:block'))

	protected readonly size = computed<ButtonVariants['size']>(() => (this.iconOnly() ? 'icon' : 'default'))

	protected readonly computedClass = computed(() => hlm(!this.iconOnly() && 'pe-2!', this.userClass()))
}
