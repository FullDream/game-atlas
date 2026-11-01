import Map from 'ol/Map.js'
import View from 'ol/View.js'
import Projection from 'ol/proj/Projection.js'
import { addCoordinateTransforms } from 'ol/proj.js'
import { remove as removeTransform } from 'ol/proj/transforms.js'
import TileGrid from 'ol/tilegrid/TileGrid.js'
import TileLayer from 'ol/layer/Tile.js'
import XYZ from 'ol/source/XYZ.js'
import type { MapGeometry, MapViewStyle } from './map-viewport-types'

export class MapEngine {
	readonly #layer = new TileLayer<XYZ>()
	readonly #map: Map
	readonly #observer: ResizeObserver
	readonly #extent: number[]
	#url: string | null = null
	#fitted = false
	#sourceProjection: Projection | undefined

	constructor(target: HTMLElement, geometry: MapGeometry) {
		if (!geometry.extent) throw new Error('Logical map geometry is required.')

		this.#extent = geometry.extent

		const projection = new Projection({
			code: `game-map:${geometry.id}:${crypto.randomUUID()}`,
			units: 'pixels',
			extent: geometry.extent,
			metersPerUnit: 1,
			getPointResolution: resolution => resolution,
		})

		const view = new View({
			projection,
			center: [0, 0],
			maxResolution: geometry.resolutions?.[0] ?? 1,
			minResolution:
				Math.min(geometry.extent[2] - geometry.extent[0], geometry.extent[3] - geometry.extent[1]) / 16384 / 4,
			zoom: 0,
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

		const logical = this.#extent
		const geometry = style?.tileGeometry
		const previousSource = this.#layer.getSource()
		const targetProjection = this.#map.getView().getProjection()

		this.#layer.setSource(null)
		previousSource?.dispose()
		this.#releaseTransforms()
		this.#layer.setExtent(logical)

		if (!url || !geometry || !style) return

		const scaleX = (logical[2] - logical[0]) / (geometry.extent[2] - geometry.extent[0])
		const scaleY = (logical[3] - logical[1]) / (geometry.extent[3] - geometry.extent[1])

		const sourceProjection = new Projection({
			code: `${targetProjection.getCode()}:style:${style.id}`,
			units: 'pixels',
			extent: geometry.extent,
			metersPerUnit: Math.min(scaleX, scaleY),
			getPointResolution: resolution => resolution,
		})

		addCoordinateTransforms(
			sourceProjection,
			targetProjection,
			coordinate => [coordinate[0] * scaleX, coordinate[1] * scaleY],
			coordinate => [coordinate[0] / scaleX, coordinate[1] / scaleY],
		)
		this.#sourceProjection = sourceProjection

		this.#layer.setSource(
			url
				? new XYZ({
						url,
						projection: sourceProjection,
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
	}

	zoom(delta: number): void {
		this.#map.getView().adjustZoom(delta)
	}

	fit(): void {
		this.#map.getView().fit(this.#extent, { size: this.#map.getSize(), padding: [24, 24, 24, 24] })
	}

	dispose(): void {
		this.#observer.disconnect()
		this.#map.setTarget(undefined)
		this.#layer.getSource()?.dispose()
		this.#releaseTransforms()
		this.#layer.dispose()
		this.#map.dispose()
	}

	#releaseTransforms(): void {
		if (!this.#sourceProjection) return

		const target = this.#map.getView().getProjection()

		removeTransform(this.#sourceProjection, target)
		removeTransform(target, this.#sourceProjection)
		this.#sourceProjection = undefined
	}
}
