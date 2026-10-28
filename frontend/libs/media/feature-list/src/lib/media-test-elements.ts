export function getTestElement<T extends Element = HTMLElement>(selector: string, root: ParentNode = document): T {
	const element = root.querySelector<T>(selector)

	if (!element) throw new Error(`Expected an element matching "${selector}".`)

	return element
}

export function getTestButton(label: string, root: ParentNode = document): HTMLButtonElement {
	const button = Array.from(root.querySelectorAll('button')).find(button => button.textContent?.trim() === label)

	if (!button) throw new Error(`Expected a button labelled "${label}".`)

	return button
}
