import { mount } from 'svelte'
import './app.css'
import App from './App.svelte'

// The app defaults to dark mode; remove this class to fall back to light.
document.documentElement.classList.add('dark')

const app = mount(App, {
  target: document.getElementById('app')!,
})

export default app
