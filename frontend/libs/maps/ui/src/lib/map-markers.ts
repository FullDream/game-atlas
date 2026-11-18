import Feature from 'ol/Feature.js'
import Point from 'ol/geom/Point.js'
import VectorSource from 'ol/source/Vector.js'
import VectorLayer from 'ol/layer/Vector.js'
import Translate from 'ol/interaction/Translate.js'
import DoubleClickZoom from 'ol/interaction/DoubleClickZoom.js'
import { asArray } from 'ol/color.js'
import { Style, Icon } from 'ol/style.js'
import Overlay from 'ol/Overlay.js'
import type OlMap from 'ol/Map.js'
import { unByKey } from 'ol/Observable.js'
import { matLocationOnFillRound } from '@ng-icons/material-symbols/round'
import { DEFAULT_TOOLTIP_CONTENT_CLASSES } from '@spartan-ng/helm/tooltip'
import type { MapMarker, MapTool, MarkerDraftState, MarkerInteractions, VisibleMapRegion } from './map-viewport-types'

const DRAFT_BADGE_COLORS: Record<MarkerDraftState, string> = {
	marker: '#F59E0B',
	category: '#0EA5E9',
	group: '#8B5CF6',
}

const DRAFT_LEVEL_LABELS: Record<MarkerDraftState, string> = {
	marker: 'marker',
	category: 'category',
	group: 'group',
}

export class MapMarkers {
	readonly #source = new VectorSource<Feature<Point>>()
	readonly #layer = new VectorLayer({ source: this.#source })
	readonly #translate: Translate
	readonly #listeners
	readonly #icons = new Map<string, Promise<HTMLCanvasElement | null>>()
	readonly #tooltipElement = document.createElement('div')
	readonly #tooltip = new Overlay({ element: this.#tooltipElement, stopEvent: false })
	readonly #leave = (): void => this.#setHovered(null)

	readonly #keydown = (event: KeyboardEvent): void => {
		if (event.key === 'Escape') this.#setHovered(null)
	}

	#disposed = false
	#tool: MapTool = 'pan'
	#origin: number[] | undefined
	#draggedFeature: Feature<Point> | undefined
	#dragging = false
	#hovered: Feature<Point> | null = null

	constructor(
		readonly map: OlMap,
		readonly interactions: MarkerInteractions,
	) {
		map.addLayer(this.#layer)
		// Canvas markers have no DOM trigger; reuse Helm tooltip styling on an OpenLayers overlay.
		this.#tooltipElement.className = `${DEFAULT_TOOLTIP_CONTENT_CLASSES} pointer-events-none max-w-60 whitespace-normal wrap-anywhere`
		this.#tooltipElement.setAttribute('role', 'tooltip')
		this.#tooltipElement.id = `map-marker-tooltip-${crypto.randomUUID()}`
		map.addOverlay(this.#tooltip)
		map.getViewport()?.addEventListener('pointerleave', this.#leave)
		map.getViewport()?.addEventListener('keydown', this.#keydown)
		this.#translate = new Translate({
			layers: [this.#layer],
			hitTolerance: 8,
			filter: feature => !feature.get('pending'),
		})
		this.#translate.setActive(false)
		map.addInteraction(this.#translate)
		this.#listeners = [
			map.on('movestart', () => this.#setHovered(null)),
			map.on('pointermove', event => {
				if (
					event.dragging ||
					this.#dragging ||
					this.#tool === 'add' ||
					('pointerType' in event.originalEvent && event.originalEvent.pointerType === 'touch')
				) {
					this.#setHovered(null)

					return
				}

				const feature = map.forEachFeatureAtPixel(
					event.pixel,
					candidate => {
						if (candidate instanceof Feature && !candidate.get('pending'))
							return candidate as Feature<Point>

						return undefined
					},
					{ layerFilter: layer => layer === this.#layer, hitTolerance: 8 },
				)

				this.#setHovered(feature ?? null)
			}),
			map.on('click', event => {
				if (this.#tool === 'add') interactions.place({ x: event.coordinate[0], y: event.coordinate[1] })
			}),
			map.on('singleclick', event => {
				if (this.#tool === 'pan') {
					const feature = map.forEachFeatureAtPixel(event.pixel, candidate => candidate, {
						layerFilter: layer => layer === this.#layer,
						hitTolerance: 8,
					})

					if (feature && !feature.get('pending')) interactions.select(String(feature.getId()))
					else if (!feature) interactions.emptyClick?.()
				}
			}),
			this.#translate.on('translatestart', event => {
				this.#setHovered(null)
				this.#dragging = true
				this.#draggedFeature = event.features.item(0) as Feature<Point>
				this.#origin = this.#draggedFeature.getGeometry()?.getCoordinates().slice()
			}),
			this.#translate.on('translateend', event => {
				this.#dragging = false
				const feature = event.features.item(0) as Feature<Point>
				const point = feature.getGeometry()?.getCoordinates()
				const original = this.#origin

				this.#draggedFeature = undefined

				this.#origin = undefined
				if (!point || !original || (point[0] === original[0] && point[1] === original[1])) return

				if (event.mapBrowserEvent.originalEvent.type === 'pointercancel') {
					feature.getGeometry()?.setCoordinates(original)

					return
				}

				interactions.move({ id: String(feature.getId()), position: { x: point[0], y: point[1] } })
			}),
		]
	}

	setTool(tool: MapTool): void {
		if (tool !== this.#tool && this.#dragging) this.cancel()

		this.#tool = tool
		this.#setHovered(null)
		this.#translate.setActive(tool === 'move')
		for (const interaction of this.map.getInteractions().getArray()) {
			if (interaction instanceof DoubleClickZoom) interaction.setActive(tool !== 'add')
		}
	}

	setMarkers(markers: MapMarker[]): void {
		const ids = new Set(markers.map(marker => marker.id))

		for (const feature of this.#source.getFeatures()) {
			if (!ids.has(String(feature.getId()))) {
				if (this.#hovered === feature) this.#setHovered(null)

				this.#source.removeFeature(feature)
			}
		}

		for (const marker of markers) {
			let feature = this.#source.getFeatureById(marker.id)

			if (!feature) {
				feature = new Feature(new Point([marker.position.x, marker.position.y]))
				feature.setId(marker.id)
				this.#source.addFeature(feature)
			} else if (!this.#dragging) feature.getGeometry()?.setCoordinates([marker.position.x, marker.position.y])

			feature.set('pending', !!marker.pending)
			feature.set('name', marker.name, true)
			feature.set('draftStates', marker.draftStates, true)
			if (marker.pending && this.#hovered === feature) this.#setHovered(null)
			else if (this.#hovered === feature) this.#showTooltip(feature)

			const draftState = marker.draftStates?.[0]
			const opacity = marker.opacity ?? (marker.pending ? 0.5 : 1)
			const signature = [
				marker.icon,
				marker.color,
				marker.selected,
				marker.pending,
				marker.failed,
				opacity,
				draftState,
			].join('|')

			if (feature.get('presentation') === signature) continue

			feature.set('presentation', signature)
			feature.set('selected', !!marker.selected, true)
			let color = marker.color

			if (color && marker.failed) color = '#dc2626'

			const size = marker.selected ? 30 : 22
			const draftBadgeSvg = draftState
				? `<circle cx="19.5" cy="4.5" r="4" fill="${DRAFT_BADGE_COLORS[draftState]}" stroke="#fff" stroke-width="1.5"/>`
				: ''
			const fallbackSvg = matLocationOnFillRound
				.replace('<svg ', '<svg width="24" height="24" ')
				.replace(/currentColor/g, color ?? '#000')
				.replace('</svg>', `${draftBadgeSvg}</svg>`)
			const fallbackUrl = `data:image/svg+xml,${encodeURIComponent(fallbackSvg)}`

			const pin = new Style({
				image: new Icon({
					src: fallbackUrl,
					scale: size / 24,
					opacity,
				}),
				zIndex: marker.selected ? 11 : 2,
			})

			feature.setStyle(pin)
			feature.set('iconPixels', 24, true)
			this.#updateHoverStyle(feature)

			const target = feature
			const baseFallbackUrl = `data:image/svg+xml,${encodeURIComponent(matLocationOnFillRound.replace('<svg ', '<svg width="24" height="24" ').replace(/currentColor/g, color ?? '#000'))}`

			void this.#maskedIcon(marker.icon || baseFallbackUrl, color, draftState)
				.then(canvas => canvas ?? this.#maskedIcon(baseFallbackUrl, color, draftState))
				.then(canvas => {
					if (
						!canvas ||
						this.#disposed ||
						this.#source.getFeatureById(marker.id) !== target ||
						target.get('presentation') !== signature
					)
						return

					const image = new Icon({
						img: canvas,
						scale: size / 64,
						opacity,
					})

					target.setStyle(new Style({ image, zIndex: marker.selected ? 11 : 2 }))
					target.set('iconPixels', 64, true)
					this.#updateHoverStyle(target)
				})
		}
	}

	reveal(id: string, region: VisibleMapRegion, duration = 0): void {
		const coordinate = this.#source.getFeatureById(id)?.getGeometry()?.getCoordinates()

		if (!coordinate) return

		const view = this.map.getView()

		view.cancelAnimations()

		const pixel = this.map.getPixelFromCoordinate(coordinate)
		const margin = 32

		if (!pixel || region.right - region.left < margin * 2 || region.bottom - region.top < margin * 2) return

		const x = Math.max(region.left + margin, Math.min(pixel[0], region.right - margin))
		const y = Math.max(region.top + margin, Math.min(pixel[1], region.bottom - margin))

		if (x === pixel[0] && y === pixel[1]) return

		const center = this.map.getView().getCenter()
		const centerPixel = center && this.map.getPixelFromCoordinate(center)

		if (!centerPixel) return

		const nextCenter = this.map.getCoordinateFromPixel([
			centerPixel[0] + pixel[0] - x,
			centerPixel[1] + pixel[1] - y,
		])

		if (duration > 0 && !matchMedia('(prefers-reduced-motion: reduce)').matches) {
			view.animate({ center: nextCenter, duration })
		} else {
			view.setCenter(nextCenter)
		}
	}

	cancel(): void {
		if (this.#origin && this.#draggedFeature) this.#draggedFeature.getGeometry()?.setCoordinates(this.#origin)

		this.#draggedFeature = undefined
		this.#dragging = false
		this.#origin = undefined
	}

	dispose(): void {
		this.#setHovered(null)

		const viewport = this.map.getViewport()

		viewport?.removeEventListener('pointerleave', this.#leave)
		viewport?.removeEventListener('keydown', this.#keydown)
		if (viewport) viewport.style.cursor = ''

		this.#disposed = true
		this.#icons.clear()
		this.cancel()
		unByKey(this.#listeners)
		this.map.removeInteraction(this.#translate)
		this.map.removeOverlay(this.#tooltip)
		this.map.removeLayer(this.#layer)
		this.#source.clear()
		this.#source.dispose()
		this.#layer.dispose()
	}

	#setHovered(feature: Feature<Point> | null): void {
		const previous = this.#hovered

		this.#hovered = feature

		const viewport = this.map.getViewport()

		if (viewport) {
			viewport.style.cursor = this.#tool === 'add' ? 'crosshair' : ''

			if (feature) viewport.style.cursor = this.#tool === 'move' ? 'grab' : 'pointer'
		}

		this.#showTooltip(feature)

		if (previous === feature) return

		if (previous) this.#updateHoverStyle(previous)

		if (feature) this.#updateHoverStyle(feature)
	}

	#showTooltip(feature: Feature<Point> | null): void {
		const coordinate = feature?.getGeometry()?.getCoordinates()
		const name = feature?.get('name') as string | undefined
		const draftStates = (feature?.get('draftStates') as readonly MarkerDraftState[] | undefined) ?? []
		const viewport = this.map.getViewport()

		if (!coordinate || !name) {
			this.#tooltip.setPosition(undefined)
			viewport?.removeAttribute('aria-describedby')

			return
		}

		const pixel = this.map.getPixelFromCoordinate(coordinate)
		const width = this.map.getSize()?.[0] ?? 0
		const above = pixel[1] >= 70
		const vertical = above ? 'bottom' : 'top'
		let horizontal: 'left' | 'center' | 'right' = 'center'

		if (pixel[0] < 130) horizontal = 'left'
		else if (pixel[0] > width - 130) horizontal = 'right'

		this.#tooltipElement.replaceChildren()

		if (draftStates.length) {
			const titleNode = document.createElement('div')
			const draftNode = document.createElement('div')

			titleNode.textContent = name
			draftNode.className = 'text-[11px] opacity-80'
			draftNode.textContent = `Draft: ${draftStates.map(state => DRAFT_LEVEL_LABELS[state]).join(', ')}`
			this.#tooltipElement.append(titleNode, draftNode)
		} else {
			this.#tooltipElement.textContent = name
		}

		this.#tooltip.setPositioning(`${vertical}-${horizontal}`)
		this.#tooltip.setOffset([0, above ? -22 : 22])
		this.#tooltip.setPosition(coordinate)
		viewport?.setAttribute('aria-describedby', this.#tooltipElement.id)
	}

	#updateHoverStyle(feature: Feature<Point>): void {
		const style = feature.getStyle()

		if (!(style instanceof Style)) return

		const image = style.getImage()
		const selected = !!feature.get('selected')
		const hovered = feature === this.#hovered
		let size = 22

		if (hovered) size = 26

		if (selected) size = 30

		if (image instanceof Icon) image.setScale(size / feature.get('iconPixels'))

		let zIndex = 2

		if (selected) zIndex = 11

		if (hovered) zIndex = 20

		style.setZIndex(zIndex)
		feature.changed()
	}

	#maskedIcon(url: string, color: string | null, draftState?: MarkerDraftState): Promise<HTMLCanvasElement | null> {
		const key = JSON.stringify([url, color, draftState ?? null])
		const cached = this.#icons.get(key)

		if (cached) return cached

		const result = new Promise<HTMLCanvasElement | null>(resolve => {
			const image = new Image()

			image.crossOrigin = 'anonymous'
			image.onerror = () => resolve(null)
			image.onload = () => {
				if (this.#disposed || !image.naturalWidth || !image.naturalHeight) {
					resolve(null)

					return
				}

				const canvas = document.createElement('canvas')

				canvas.width = canvas.height = 64

				const context = canvas.getContext('2d')

				if (!context) {
					resolve(null)

					return
				}

				const scale = 64 / Math.max(image.naturalWidth, image.naturalHeight)
				const width = image.naturalWidth * scale
				const height = image.naturalHeight * scale

				context.drawImage(image, (64 - width) / 2, (64 - height) / 2, width, height)

				if (color === null) {
					if (draftState) this.#drawDraftDot(context, 52, 12, 9, 3, DRAFT_BADGE_COLORS[draftState])

					resolve(canvas)

					return
				}

				context.globalCompositeOperation = 'source-in'
				context.fillStyle = color
				context.fillRect(0, 0, 64, 64)

				const outlined = document.createElement('canvas')

				outlined.width = outlined.height = 96

				const outline = outlined.getContext('2d')

				if (!outline) {
					resolve(canvas)

					return
				}

				const [red, green, blue] = asArray(color)
					.slice(0, 3)
					.map(channel => {
						const value = channel / 255

						return value <= 0.04045 ? value / 12.92 : Math.pow((value + 0.055) / 1.055, 2.4)
					})
				const luminance = 0.2126 * red + 0.7152 * green + 0.0722 * blue

				const radius = (0.75 * 64) / 22

				for (let step = 0; step < 16; step++) {
					const angle = (step * Math.PI * 2) / 16

					outline.drawImage(canvas, 16 + Math.cos(angle) * radius, 16 + Math.sin(angle) * radius)
				}

				outline.globalCompositeOperation = 'source-in'
				outline.fillStyle = luminance > 0.179 ? '#000' : '#fff'
				outline.fillRect(0, 0, 96, 96)
				outline.globalCompositeOperation = 'source-over'
				outline.drawImage(canvas, 16, 16)

				if (draftState) this.#drawDraftDot(outline, 68, 28, 10, 3.5, DRAFT_BADGE_COLORS[draftState])

				resolve(outlined)
			}
			image.src = url
		})

		this.#icons.set(key, result)

		return result
	}

	#drawDraftDot(
		context: CanvasRenderingContext2D,
		cx: number,
		cy: number,
		radius: number,
		strokeWidth: number,
		fillColor: string,
	): void {
		context.save()
		context.globalCompositeOperation = 'source-over'
		context.beginPath()
		context.arc(cx, cy, radius, 0, Math.PI * 2)
		context.fillStyle = fillColor
		context.fill()
		context.lineWidth = strokeWidth
		context.strokeStyle = '#ffffff'
		context.stroke()
		context.restore()
	}
}
