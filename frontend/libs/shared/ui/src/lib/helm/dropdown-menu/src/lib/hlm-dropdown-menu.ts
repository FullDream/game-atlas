import { type NumberInput } from '@angular/cdk/coercion'
import { CdkMenu } from '@angular/cdk/menu'
import { Directive, ElementRef, inject, input, numberAttribute, signal } from '@angular/core'
import { takeUntilDestroyed } from '@angular/core/rxjs-interop'
import { deriveMenuSideFromTransformOrigin, MENU_SIDE, type MenuSide } from '@spartan-ng/brain/core'
import { classes } from '@spartan-ng/helm/utils'

@Directive({
	selector: '[hlmDropdownMenu],hlm-dropdown-menu',
	exportAs: 'hlmDropdownMenu',
	hostDirectives: [CdkMenu],
	host: {
		'data-slot': 'dropdown-menu',
		'[attr.data-state]': 'state()',
		'[attr.data-side]': 'side()',
		'[style.--side-offset]': 'sideOffset()',
	},
})
export class HlmDropdownMenu {
	readonly #host = inject(CdkMenu)
	readonly #elementRef = inject(ElementRef<HTMLElement>)
	// The trigger provides its configured side; CDK parents this content's injector under the trigger's.
	readonly #menuSide = inject(MENU_SIDE, { optional: true })

	protected readonly state = signal('open')
	protected readonly side = signal<MenuSide>(this.#menuSide?.side() ?? 'bottom')

	public readonly sideOffset = input<number, NumberInput>(1, { transform: numberAttribute })

	constructor() {
		classes(
			() =>
				'motion-safe:data-open:animate-in motion-safe:data-closed:animate-out data-closed:fade-out-0 data-open:fade-in-0 data-closed:zoom-out-95 data-open:zoom-in-95 data-[side=bottom]:slide-in-from-top-2 data-[side=left]:slide-in-from-right-2 data-[side=right]:slide-in-from-left-2 data-[side=top]:slide-in-from-bottom-2 ring-foreground/10 bg-popover text-popover-foreground min-w-32 rounded-md p-1 shadow-md my-[--spacing(var(--side-offset))] overflow-x-hidden overflow-y-auto ring-1 duration-100 outline-none',
		)

		this.#setSideFromTransformOrigin()
		// this is a best effort, but does not seem to work currently
		// TODO: figure out a way for us to know the host is about to be closed. might not be possible with CDK
		this.#host.closed.pipe(takeUntilDestroyed()).subscribe(() => this.state.set('closed'))
	}

	#setSideFromTransformOrigin(): void {
		const side = this.#menuSide?.side() ?? 'bottom'

		// CDK sets transform-origin on this element synchronously on attach; read it next tick and derive side
		setTimeout(() => {
			this.side.set(deriveMenuSideFromTransformOrigin(this.#elementRef.nativeElement.style.transformOrigin, side))
		})
	}
}
