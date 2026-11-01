import Map from 'ol/Map.js'
import View from 'ol/View.js'
import Projection from 'ol/proj/Projection.js'
import TileGrid from 'ol/tilegrid/TileGrid.js'
import TileLayer from 'ol/layer/Tile.js'
import XYZ from 'ol/source/XYZ.js'
import type { MapGeometry, MapViewStyle } from './map-viewport-types'

export class MapEngine {
	readonly #layer = new TileLayer<XYZ>()
	readonly #map: Map
	readonly #observer: ResizeObserver
	readonly #geometry: MapGeometry
	#url: string | null = null
	#fitted = false

	constructor(target: HTMLElement, geometry: MapGeometry) {
		this.#geometry = geometry

		const projection = new Projection({ code: `game-map:${geometry.id}`, units: 'pixels', extent: geometry.extent })

		const view = new View({
			projection,
			center: [0, 0],
			resolutions: geometry.resolutions,
			minZoom: geometry.minZoom,
			maxZoom: geometry.maxZoom,
			zoom: geometry.minZoom,
			enableRotation: false,
			constrainResolution: false,
		})

		this.#map = new Map({ target, layers: [this.#layer], view, controls: [] })

		this.#observer = new ResizeObserver(() => {
			this.#map.updateSize()

			if (!this.#fitted && target.clientWidth && target.clientHeight) {
				this.fit()
				this.#fitted = true
			}
		})

		this.#observer.observe(target)
	}

	setStyle(style: MapViewStyle | undefined): void {
		const url = style?.tilesUrlTemplate ?? null

		if (url === this.#url) return

		this.#url = url

		const geometry = this.#geometry
		const previousSource = this.#layer.getSource()

		this.#layer.setSource(
			url
				? new XYZ({
						url,
						projection: this.#map.getView().getProjection(),
						wrapX: false,
						maxZoom: geometry.maxZoom,
						tileGrid: new TileGrid({
							extent: geometry.extent,
							origin: geometry.tileOrigin,
							tileSize: geometry.tileSize,
							resolutions: geometry.resolutions,
							minZoom: geometry.minZoom,
						}),
					})
				: null,
		)

		previousSource?.dispose()
	}

	zoom(delta: number): void {
		this.#map.getView().adjustZoom(delta)
	}

	fit(): void {
		this.#map.getView().fit(this.#geometry.extent, { size: this.#map.getSize(), padding: [24, 24, 24, 24] })
	}

	dispose(): void {
		this.#observer.disconnect()
		this.#map.setTarget(undefined)
		this.#layer.getSource()?.dispose()
		this.#layer.dispose()
		this.#map.dispose()
	}
}
