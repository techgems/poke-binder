import { mount } from 'svelte'
import './app.css'
import App from './App.svelte'
import { preloadedBinder } from './preloads/preload'
import { binderPage } from './stores/binder-page.svelte'
import { save } from './stores/save.svelte'

// Before the mount, not after: the Razor page embedded this binder above the app's own scripts, so
// it is already here, and seeding first means the first paint is the real binder rather than an
// empty sheet that fills in a frame later. Null when the page was opened without a binder.
const preloaded = preloadedBinder()

if (preloaded) {
  binderPage.load(preloaded)

  // Registered here rather than on import, so pulling the save store into a module has no side
  // effect of its own -- and only for a page that actually has a binder to save.
  save.listenForUnload()
}

const app = mount(App, {
  target: document.getElementById('app')!,
})

export default app
