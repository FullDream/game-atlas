import { Component, computed, input, output, signal } from '@angular/core'
import type { Category, Marker } from '@game-atlas/maps/data-access'
import { CategoryIcon } from '@game-atlas/maps/ui'
import { Button, ErrorState, MediaThumbnail, TooltipOnOverflow } from '@game-atlas/shared/ui'
import { HlmAccordionImports } from '@spartan-ng/helm/accordion'
import { HlmEmptyImports } from '@spartan-ng/helm/empty'
import { HlmSkeleton } from '@spartan-ng/helm/skeleton'
import { HlmTooltip } from '@spartan-ng/helm/tooltip'

@Component({
	selector: 'maps-markers',
	imports: [
		CategoryIcon,
		Button,
		ErrorState,
		MediaThumbnail,
		TooltipOnOverflow,
		HlmAccordionImports,
		HlmEmptyImports,
		HlmSkeleton,
		HlmTooltip,
	],
	templateUrl: './map-markers.html',
	host: {
		class: 'flex min-h-14 basis-14 shrink-0 flex-col overflow-hidden transition-[flex-grow] duration-200 motion-reduce:transition-none',
		'[style.flex-grow]': 'sectionOpen() ? 1 : 0',
	},
})
export class MapMarkers {
	readonly markers = input<Marker[]>([])
	readonly categories = input.required<ReadonlyMap<string, Category>>()
	readonly selectedId = input('')
	readonly pendingMarkerIds = input.required<ReadonlySet<string>>()
	readonly loading = input(false)
	readonly error = input(false)

	readonly markerSelected = output<string>()
	readonly retryRequested = output<void>()

	readonly sectionOpen = signal(true)

	protected readonly rows = computed(() => {
		const categories = this.categories()

		return this.markers().map(marker => ({
			marker,
			category: categories.get(marker.categoryId),
			photos: marker.media.filter(media => media.type === 'image').slice(0, 3),
		}))
	})
}
