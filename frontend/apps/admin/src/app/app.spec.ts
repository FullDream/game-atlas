import { ComponentFixture, TestBed } from '@angular/core/testing'
import { Router, provideRouter, TitleStrategy, withComponentInputBinding } from '@angular/router'
import { Title } from '@angular/platform-browser'
import { provideLocalStorageImpl } from 'ngxtension/inject-local-storage'
import { HlmSidebarService } from '@spartan-ng/helm/sidebar'
import { provideSpartanHlm } from '@spartan-ng/helm/utils'
import { App } from './app'
import { appRoutes } from './app.routes'
import { AdminTitleStrategy } from './admin-title-strategy'

async function render(path = '/games'): Promise<ComponentFixture<App>> {
	const fixture = TestBed.createComponent(App)

	await TestBed.inject(Router).navigateByUrl(path)
	await fixture.whenStable()
	fixture.detectChanges()

	return fixture
}

describe('Administration shell', () => {
	beforeEach(async () => {
		localStorage.clear()
		document.head.insertAdjacentHTML(
			'beforeend',
			`
			<meta id="theme-color-light" name="theme-color" content="#ffffff" media="(prefers-color-scheme: light)">
			<meta id="theme-color-dark" name="theme-color" content="#09090b" media="(prefers-color-scheme: dark)">
		`,
		)

		await TestBed.configureTestingModule({
			imports: [App],
			providers: [
				provideRouter(appRoutes, withComponentInputBinding()),
				{ provide: TitleStrategy, useClass: AdminTitleStrategy },
				provideSpartanHlm(),
				provideLocalStorageImpl(localStorage),
			],
		}).compileComponents()
	})

	afterEach(() => {
		vi.unstubAllGlobals()
		localStorage.clear()
		document.head
			.querySelectorAll('meta[name="color-scheme"], meta[name="theme-color"]')
			.forEach(meta => meta.remove())
	})

	it('redirects to Games and exposes honest foundation content without API data', async () => {
		const fixture = await render('/')
		const element = fixture.nativeElement as HTMLElement

		expect(TestBed.inject(Router).url).toBe('/games')
		expect(TestBed.inject(Title).getTitle()).toBe('Games | GameAtlas Admin')
		expect(element.querySelector('h1')?.textContent).toBe('Games')
		expect(element.textContent).toContain('Game management will be available here.')
		expect(element.querySelector('a[aria-current="page"]')?.getAttribute('aria-label')).toBe('Games')
	})

	it('updates heading, breadcrumb and active navigation on route changes', async () => {
		const fixture = await render()

		await TestBed.inject(Router).navigateByUrl('/media')
		await fixture.whenStable()
		const element = fixture.nativeElement as HTMLElement

		expect(element.querySelector('h1')?.textContent).toBe('Media')
		expect(TestBed.inject(Title).getTitle()).toBe('Media | GameAtlas Admin')
		expect(element.querySelector('nav[aria-label="Breadcrumb"]')?.textContent).toContain('Media')
		expect(element.querySelector('a[aria-current="page"]')?.getAttribute('aria-label')).toBe('Media')
	})

	it('toggles desktop navigation through its named control', async () => {
		const fixture = await render()
		const element = fixture.nativeElement as HTMLElement
		const toggle = element.querySelector<HTMLButtonElement>('button[hlmSidebarTrigger]')

		expect(toggle?.textContent).toContain('Toggle navigation')
		expect(toggle?.getAttribute('aria-expanded')).toBe('true')
		toggle?.click()
		await fixture.whenStable()

		expect(toggle?.getAttribute('aria-expanded')).toBe('false')
		expect(element.querySelector('hlm-sidebar')?.getAttribute('data-state')).toBe('collapsed')
	})

	it('closes mobile navigation when a destination is selected', async () => {
		vi.stubGlobal(
			'matchMedia',
			vi.fn().mockImplementation(() => ({
				matches: true,
				addEventListener: vi.fn(),
				removeEventListener: vi.fn(),
			})),
		)
		const fixture = await render()
		const sidebar = fixture.debugElement.injector.get(HlmSidebarService)

		expect(sidebar.isMobile()).toBe(true)
		sidebar.setOpenMobile(true)
		await fixture.whenStable()
		const media = document.querySelector<HTMLAnchorElement>('[data-mobile="true"] a[aria-label="Media"]')

		expect(media).not.toBeNull()
		media?.click()
		await fixture.whenStable()

		expect(sidebar.openMobile()).toBe(false)
		expect(TestBed.inject(Router).url).toBe('/media')
	})

	it('dismisses mobile navigation with Escape and restores focus to its trigger', async () => {
		vi.stubGlobal(
			'matchMedia',
			vi.fn().mockImplementation(() => ({
				matches: true,
				addEventListener: vi.fn(),
				removeEventListener: vi.fn(),
			})),
		)
		const fixture = await render()
		const toggle = fixture.nativeElement.querySelector('button[hlmSidebarTrigger]') as HTMLButtonElement
		const sidebar = fixture.debugElement.injector.get(HlmSidebarService)

		toggle.focus()
		toggle.click()
		await fixture.whenStable()
		expect(sidebar.openMobile()).toBe(true)
		expect(document.querySelector('[role="dialog"]')?.contains(document.activeElement)).toBe(true)

		document.activeElement?.dispatchEvent(
			new KeyboardEvent('keydown', { key: 'Escape', code: 'Escape', bubbles: true }),
		)
		await fixture.whenStable()
		expect(sidebar.openMobile()).toBe(false)
		expect(document.activeElement).toBe(toggle)
	})

	it('renders a not-found state inside the shell', async () => {
		const fixture = await render('/missing')

		expect(fixture.nativeElement.querySelector('h1')?.textContent).toBe('Page not found')
	})

	it('defaults to both color schemes with an accessible three-option switcher', async () => {
		const fixture = await render()
		const switcher = fixture.nativeElement.querySelector('ui-theme-switcher') as HTMLElement

		expect(switcher.getAttribute('aria-label')).toBe('Color theme')
		expect(switcher.getAttribute('role')).toBe('radiogroup')
		expect(switcher.querySelectorAll('input[type="radio"]')).toHaveLength(3)
		expect(
			new Set(Array.from(switcher.querySelectorAll<HTMLInputElement>('input'), radio => radio.name)).size,
		).toBe(1)
		expect(switcher.querySelector<HTMLInputElement>('input:checked')?.value).toBe('system')
		expect(document.head.querySelector('meta[name="color-scheme"]')?.getAttribute('content')).toBe('light dark')
		expect(document.head.querySelector('#theme-color-light')?.getAttribute('media')).toBe(
			'(prefers-color-scheme: light)',
		)
		expect(document.head.querySelector('#theme-color-dark')?.getAttribute('media')).toBe(
			'(prefers-color-scheme: dark)',
		)
	})

	it.each([
		['light', 'light', 'all', 'not all'],
		['dark', 'dark', 'not all', 'all'],
		['system', 'light dark', '(prefers-color-scheme: light)', '(prefers-color-scheme: dark)'],
		['invalid', 'light dark', '(prefers-color-scheme: light)', '(prefers-color-scheme: dark)'],
	])('restores stored preference %s as color-scheme %s', async (preference, scheme, lightMedia, darkMedia) => {
		localStorage.setItem('game-atlas.theme', preference)

		await render()

		expect(document.head.querySelector('meta[name="color-scheme"]')?.getAttribute('content')).toBe(scheme)
		expect(document.head.querySelector('#theme-color-light')?.getAttribute('media')).toBe(lightMedia)
		expect(document.head.querySelector('#theme-color-dark')?.getAttribute('media')).toBe(darkMedia)
	})

	it('persists theme selection and returns to system mode', async () => {
		const fixture = await render()
		const element = fixture.nativeElement as HTMLElement

		element.querySelector<HTMLInputElement>('ui-theme-switcher input[value="dark"]')?.click()
		await fixture.whenStable()

		expect(localStorage.getItem('game-atlas.theme')).toBe('dark')
		expect(document.head.querySelector('#theme-color-dark')?.getAttribute('media')).toBe('all')
		expect(document.head.querySelector('meta[name="color-scheme"]')?.getAttribute('content')).toBe('dark')
		expect(element.querySelector<HTMLInputElement>('ui-theme-switcher input[value="dark"]')?.checked).toBe(true)

		element.querySelector<HTMLInputElement>('ui-theme-switcher input[value="system"]')?.click()
		await fixture.whenStable()

		expect(localStorage.getItem('game-atlas.theme')).toBe('system')
		expect(document.head.querySelector('#theme-color-dark')?.getAttribute('media')).toBe(
			'(prefers-color-scheme: dark)',
		)
		expect(document.head.querySelector('meta[name="color-scheme"]')?.getAttribute('content')).toBe('light dark')
	})

	it('reflects theme changes from another tab', async () => {
		const fixture = await render()

		localStorage.setItem('game-atlas.theme', 'light')
		window.dispatchEvent(
			new StorageEvent('storage', {
				key: 'game-atlas.theme',
				newValue: 'light',
				storageArea: localStorage,
			}),
		)
		await fixture.whenStable()

		expect(document.head.querySelector('meta[name="color-scheme"]')?.getAttribute('content')).toBe('light')
		expect(document.head.querySelector('#theme-color-light')?.getAttribute('media')).toBe('all')
		expect(fixture.nativeElement.querySelector('ui-theme-switcher input[value="light"]')?.checked).toBe(true)
	})
})
