import { RenderMode, ServerRoute } from '@angular/ssr'

export const serverRoutes: ServerRoute[] = [
	// Future parameterized routes go before this fallback and use
	// getPrerenderParams() backed by the published content manifest.
	{
		path: '**',
		renderMode: RenderMode.Prerender,
	},
]
