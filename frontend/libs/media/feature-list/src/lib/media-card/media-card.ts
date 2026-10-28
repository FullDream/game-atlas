import { booleanAttribute, Component, computed, input, output } from '@angular/core'
import { RouterLink } from '@angular/router'
import type { MediaItemDto } from '@game-atlas/media/data-access'
import { Button, TooltipOnOverflow } from '@game-atlas/shared/ui'
import { NgIcon, provideIcons } from '@ng-icons/core'
import {
	matDeleteRound,
	matDriveFileMoveRound,
	matEditRound,
	matFolderOpenRound,
	matFolderRound,
	matMoreVertRound,
	matVideoLibraryRound,
} from '@ng-icons/material-symbols/round'
import { HlmContextMenuImports } from '@spartan-ng/helm/context-menu'
import { HlmDropdownMenuImports } from '@spartan-ng/helm/dropdown-menu'
import { HlmSkeleton } from '@spartan-ng/helm/skeleton'
import { HlmTooltip } from '@spartan-ng/helm/tooltip'
import { MediaThumbnail } from '../media-thumbnail/media-thumbnail'

@Component({
	selector: 'media-card',
	imports: [
		RouterLink,
		TooltipOnOverflow,
		HlmTooltip,
		NgIcon,
		Button,
		HlmContextMenuImports,
		HlmDropdownMenuImports,
		MediaThumbnail,
		HlmSkeleton,
	],
	templateUrl: './media-card.html',
	providers: [
		provideIcons({
			matFolderRound,
			matFolderOpenRound,
			matVideoLibraryRound,
			matMoreVertRound,
			matEditRound,
			matDriveFileMoveRound,
			matDeleteRound,
		}),
	],
	host: {
		role: 'article',
		class: 'group block min-w-0 rounded-lg bg-card hover:shadow-sm relative overflow-hidden border transition-[border-color,box-shadow] delay-0 data-[drop-allowed=true]:border-primary/50 data-[drop-active=true]:delay-[160ms] data-[drop-active=true]:ring-2 data-[drop-active=true]:ring-primary',
		'[attr.data-drop-allowed]': 'dropAllowed()',
		'[attr.data-drop-active]': 'dropActive()',
		'[attr.data-skeleton]': 'skeleton() || null',
		'[attr.aria-hidden]': 'skeleton() || null',
	},
})
export class MediaCard {
	readonly item = input<MediaItemDto>()
	readonly skeleton = input(false, { transform: booleanAttribute })
	readonly limit = input(24)
	readonly busy = input(false)
	readonly dropAllowed = input(false)
	readonly dropActive = input(false)

	readonly opened = output<MouseEvent>()
	readonly renamed = output()
	readonly moved = output()
	readonly deleted = output()

	protected readonly folderIcon = computed(() => {
		if (this.dropActive()) return 'matFolderOpenRound'

		if (this.dropAllowed()) return 'matDriveFileMoveRound'

		return 'matFolderRound'
	})
}
