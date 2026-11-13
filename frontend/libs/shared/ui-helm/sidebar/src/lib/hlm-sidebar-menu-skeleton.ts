import { type BooleanInput } from '@angular/cdk/coercion'
import { booleanAttribute, Component, input } from '@angular/core'
import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton'
import { classes } from '@spartan-ng/helm/utils'

@Component({
	selector: 'hlm-sidebar-menu-skeleton,div[hlmSidebarMenuSkeleton]',
	exportAs: 'hlmSidebarMenuSkeleton',
	imports: [HlmSkeletonImports],
	templateUrl: './hlm-sidebar-menu-skeleton.html',
	host: {
		'data-slot': 'sidebar-menu-skeleton',
		'data-sidebar': 'menu-skeleton',
	},
})
export class HlmSidebarMenuSkeleton {
	public readonly showIcon = input<boolean, BooleanInput>(false, { transform: booleanAttribute })

	protected readonly width = `${Math.floor(Math.random() * 40) + 50}%`

	constructor() {
		classes(() => 'h-8 gap-2 rounded-md px-2 flex items-center')
	}
}
