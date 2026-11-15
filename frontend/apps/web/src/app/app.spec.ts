import { TestBed } from '@angular/core/testing'
import { provideRouter } from '@angular/router'
import { provideSpartanHlm } from '@spartan-ng/helm/utils'
import { App } from './app'

describe('App', () => {
	it('renders the public shell header with brand link and compact theme switcher', async () => {
		await TestBed.configureTestingModule({
			imports: [App],
			providers: [provideRouter([]), provideSpartanHlm()],
		}).compileComponents()

		const fixture = TestBed.createComponent(App)

		await fixture.whenStable()

		const root = fixture.nativeElement as HTMLElement

		expect(root.querySelector('header a')?.textContent).toContain('GameAtlas')
		expect(root.querySelector('header ui-theme-switcher')).not.toBeNull()
		expect(root.querySelector('main#main-content router-outlet')).not.toBeNull()
	})
})
