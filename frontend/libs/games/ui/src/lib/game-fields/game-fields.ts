import { formatDate } from '@angular/common'
import { booleanAttribute, Component, input, output } from '@angular/core'
import { FormField, type FieldTree } from '@angular/forms/signals'
import { NgIcon, provideIcons } from '@ng-icons/core'
import {
	matImageRound,
	matFormatBoldRound,
	matFormatItalicRound,
	matLinkRound,
	matFormatListBulletedRound,
} from '@ng-icons/material-symbols/round'
import { HlmSelectImports } from '@spartan-ng/helm/select'
import { HlmInput } from '@spartan-ng/helm/input'
import { HlmInputGroupImports } from '@spartan-ng/helm/input-group'
import { HlmToggleGroupImports } from '@spartan-ng/helm/toggle-group'
import { HlmSeparator } from '@spartan-ng/helm/separator'
import { HlmDatePicker, HlmDatePickerInput } from '@spartan-ng/helm/date-picker'
import { HlmSkeleton } from '@spartan-ng/helm/skeleton'
import { Button, MediaThumbnail, Field } from '@game-atlas/shared/ui'
import { type LibraryImage } from '@game-atlas/shared/util'
import { type GameForm } from './game-form'

@Component({
	selector: 'games-fields',
	imports: [
		Field,
		NgIcon,
		FormField,
		HlmSelectImports,
		HlmInput,
		HlmInputGroupImports,
		HlmToggleGroupImports,
		HlmSeparator,
		HlmSkeleton,
		HlmDatePicker,
		HlmDatePickerInput,
		Button,
		MediaThumbnail,
	],
	templateUrl: './game-fields.html',
	styleUrl: './game-fields.css',
	providers: [
		provideIcons({
			matImageRound,
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
	readonly banner = input<LibraryImage | null>()
	readonly editing = input(false, { transform: booleanAttribute })
	readonly busy = input(false, { transform: booleanAttribute })

	readonly chooseCover = output<void>()
	readonly chooseBanner = output<void>()
	readonly removeBanner = output<void>()
	readonly statusChange = output<unknown>()

	protected readonly formatReleaseDate = (date: Date): string => formatDate(date, 'dd.MM.yyyy', 'en-US')

	protected readonly parseReleaseDate = (value: string): Date | null => {
		const match = /^(\d{2})\.(\d{2})\.(\d{4})$/.exec(value)

		if (!match) return null

		const day = Number(match[1])
		const month = Number(match[2]) - 1
		const year = Number(match[3])
		const date = new Date(0)

		date.setFullYear(year, month, day)
		date.setHours(0, 0, 0, 0)

		return date.getFullYear() === year && date.getMonth() === month && date.getDate() === day ? date : null
	}

	protected readonly statusLabel = (value: unknown): string => (value === 'published' ? 'Published' : 'Draft')
}
