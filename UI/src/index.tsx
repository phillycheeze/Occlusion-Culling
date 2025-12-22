import { ModRegistrar } from "cs2/modding";
import "mod.json";

// Create a <style> element that sets CSS variables in :root
const createOverrideStyle = (vars: [string, string][]) => {
    const style = document.createElement("style");
    style.type = "text/css";
    const body = vars.map(([k, v]) => `        ${k}: ${v} !important;`).join("\n");
    style.innerHTML = `:root {\n${body}\n    }`;
    return style;
};

// Escape a string for use inside a RegExp
const escapeRegex = (s: string) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

// Remove matching CSS variable declarations from an element's inline style
const removeRulesFromHeadStyle = (el: HTMLElement, vars: [string, string][]) => {
    let cssText = el.getAttribute("style") || "";
    for (const [key] of vars) {
        const re = new RegExp(`${escapeRegex(key)}\\s*:\\s*[^;]+;?`, "g");
        cssText = cssText.replace(re, "");
    }
    // clean up extra whitespace/semicolons
    cssText = cssText.replace(/\s{2,}/g, " ").trim();
    if (cssText) {
        el.setAttribute("style", cssText);
    } else {
        el.removeAttribute("style");
    }
};

// List of CSS variables to override and their desired values
const overrides: [string, string][] = [
    ['--panelBlur', 'none'],
    ['--panelOpacityNormal', '1'],
    ['--panelOpacityDark', '1'],
];

// Mod entry: inject override style and observe <html> for inline style changes
const register: ModRegistrar = (moduleRegistry) => {
    // document.documentElement is the <html> element
    const head = document.documentElement;

    const mutationCallback: MutationCallback = (mutationList: MutationRecord[]) => {
        for (const mutation of mutationList) {
            if (mutation.type === "attributes" && mutation.attributeName == "style") {
                // If any override variable appears in the element's inline style, remove it
                if (overrides.some(([k]) => head.style.cssText.includes(k))) {
                    removeRulesFromHeadStyle(head, overrides);
                }
            }
        }
    };

    const observer = new MutationObserver(mutationCallback);
    observer.observe(head, { attributes: true, childList: false, subtree: false });

    const style = createOverrideStyle(overrides);
    head.appendChild(style);
};

export default register;
