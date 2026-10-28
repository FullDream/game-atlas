import { DIALOG_DATA } from '@angular/cdk/dialog'
import { signal } from '@angular/core'
import { TestBed } from '@angular/core/testing'
import { BrnDialogRef } from '@spartan-ng/brain/dialog'
import { provideSpartanHlm } from '@spartan-ng/helm/utils'
import { getTestButton } from '../media-test-elements'
import { ConfirmationDialog } from './confirmation-dialog'

describe('ConfirmationDialog', () => {
	it.each([
		['Cancel', false],
		['Delete', true],
	])('returns the decision when %s is activated', async (label, result) => {
		const close = vi.fn()

		TestBed.configureTestingModule({
			providers: [
				provideSpartanHlm(),
				{ provide: BrnDialogRef, useValue: { close, state: signal('open') } },
				{
					provide: DIALOG_DATA,
					useValue: {
						title: 'Delete file?',
						description: 'This file will be permanently deleted.',
						confirm: 'Delete',
						cancel: 'Cancel',
						destructive: true,
					},
				},
			],
		})
		const fixture = TestBed.createComponent(ConfirmationDialog)

		await fixture.whenStable()

		expect(fixture.nativeElement.querySelector('h2')?.textContent).toBe('Delete file?')
		expect(fixture.nativeElement.textContent).toContain('This file will be permanently deleted.')
		getTestButton(label, fixture.nativeElement).click()

		expect(close).toHaveBeenCalledExactlyOnceWith(result)
	})
})
