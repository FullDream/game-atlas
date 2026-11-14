import { Component, computed, input, output, model } from '@angular/core'
import type { Category, CategoryGroup, Marker } from '@game-atlas/maps/data-access'
import { CategoryIcon } from '@game-atlas/maps/ui'
import { Button, ErrorState, MediaThumbnail, TooltipOnOverflow } from '@game-atlas/shared/ui'
import { NgIcon, provideIcons } from '@ng-icons/core'
import { matLocationOnRound } from '@ng-icons/material-symbols/round'
import { HlmAccordionImports } from '@spartan-ng/helm/accordion'
import { HlmBadge } from '@spartan-ng/helm/badge'
import { HlmEmptyImports } from '@spartan-ng/helm/empty'
import { HlmSkeleton } from '@spartan-ng/helm/skeleton'
import { HlmTooltip } from '@spartan-ng/helm/tooltip'

@Component({
	selector: 'maps-markers',
	imports: [
		NgIcon,
		CategoryIcon,
		Button,
		ErrorState,
		MediaThumbnail,
		TooltipOnOverflow,
		HlmAccordionImports,
		HlmBadge,
		HlmEmptyImports,
		HlmSkeleton,
		HlmTooltip,
	],
	templateUrl: './map-markers.html',
	providers: [provideIcons({ matLocationOnRound })],
	host: {
		class: 'flex min-h-10 basis-10 shrink-0 flex-col overflow-hidden transition-[flex-grow] duration-200 motion-reduce:transition-none',
		'[style.flex-grow]': 'sectionOpen() ? 1 : 0',
	},
})
export class MapMarkers {
	readonly markers = input<Marker[]>([])
	readonly categories = input.required<ReadonlyMap<string, Category>>()
	readonly groups = input<ReadonlyMap<string, CategoryGroup>>(new Map())
	readonly selectedId = input('')
	readonly pendingMarkerIds = input.required<ReadonlySet<string>>()
	readonly loading = input(false)
	readonly error = input(false)

	readonly markerSelected = output<string>()
	readonly retryRequested = output<void>()

	readonly sectionOpen = model(false)

	protected readonly rows = computed(() => {
		const categories = this.categories()
		const groups = this.groups()

		return this.markers().map(marker => {
			const category = categories.get(marker.categoryId)
			const group = category ? groups.get(category.categoryGroupId) : undefined

			return {
				marker,
				category,
				isDraft: marker.status === 'draft' || category?.status === 'draft' || group?.status === 'draft',
				photos: marker.media.filter(media => media.type === 'image').slice(0, 3),
			}
		})
	})
}
