import { mount } from 'svelte'
import './app.css'
import App from './App.svelte'
import { preloadedBinder } from './clients/preload'
import { binderPage } from './lib/binder-page.svelte'

// Before the mount, not after: the Razor page embedded this binder above the app's own scripts, so
// it is already here, and seeding first means the first paint is the real binder rather than an
// empty sheet that fills in a frame later. Null when the page was opened without a binder.
const preloaded = preloadedBinder()

if (preloaded) {
  binderPage.load(preloaded)
}

const app = mount(App, {
  target: document.getElementById('app')!,
})

export default app
