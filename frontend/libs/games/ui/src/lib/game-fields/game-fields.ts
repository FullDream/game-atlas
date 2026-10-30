import { booleanAttribute, Component, input, output } from '@angular/core'
import { FormField, type FieldTree } from '@angular/forms/signals'
import { NgIcon, provideIcons } from '@ng-icons/core'
import {
	matImageRound,
	matCalendarMonthRound,
	matFormatBoldRound,
	matFormatItalicRound,
	matLinkRound,
	matFormatListBulletedRound,
} from '@ng-icons/material-symbols/round'
import { HlmFieldImports } from '@spartan-ng/helm/field'
import { HlmSelectImports } from '@spartan-ng/helm/select'
import { HlmInput } from '@spartan-ng/helm/input'
import { HlmInputGroupImports } from '@spartan-ng/helm/input-group'
import { HlmToggleGroupImports } from '@spartan-ng/helm/toggle-group'
import { HlmSeparator } from '@spartan-ng/helm/separator'
import { HlmSkeleton } from '@spartan-ng/helm/skeleton'
import { Button, MediaThumbnail } from '@game-atlas/shared/ui'
import { type LibraryImage } from '@game-atlas/shared/util'
import { type GameForm } from './game-form'

@Component({
	selector: 'games-fields',
	imports: [
		NgIcon,
		FormField,
		HlmFieldImports,
		HlmSelectImports,
		HlmInput,
		HlmInputGroupImports,
		HlmToggleGroupImports,
		HlmSeparator,
		HlmSkeleton,
		Button,
		MediaThumbnail,
	],
	templateUrl: './game-fields.html',
	styleUrl: './game-fields.css',
	providers: [
		provideIcons({
			matImageRound,
			matCalendarMonthRound,
			matFormatBoldRound,
			matFormatItalicRound,
			matLinkRound,
			matFormatListBulletedRound,
		}),
	],
	host: {
		role: 'region',
		'aria-label': 'Basic information',
		class: 'min-w-0 rounded-xl border-border bg-card p-4 text-card-foreground md:p-6 block border',
	},
})
export class GameFields {
	readonly fields = input<FieldTree<GameForm>>()
	readonly skeleton = input(false, { transform: booleanAttribute })
	readonly cover = input<LibraryImage>()
	readonly editing = input(false, { transform: booleanAttribute })
	readonly busy = input(false, { transform: booleanAttribute })

	readonly chooseCover = output<void>()
	readonly statusChange = output<unknown>()

	protected readonly statusLabel = (value: unknown): string => (value === 'published' ? 'Published' : 'Draft')
}
