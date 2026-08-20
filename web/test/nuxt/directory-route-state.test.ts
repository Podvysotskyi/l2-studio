import { describe, expect, it, vi } from 'vitest'
import { defineComponent, ref } from 'vue'
import { mountSuspended } from '@nuxt/test-utils/runtime'
import { useDirectoryRouteSync } from '../../app/composables/use-directory-route-sync'

const DirectoryProbe = defineComponent({
  setup() {
    const query = ref('')
    const page = ref(1)
    const pageSize = ref(25)
    const load = vi.fn(async () => undefined)
    useDirectoryRouteSync('/library/maps', { query, page, pageSize }, load)
    return { query, page, pageSize, load }
  },
  template: `
    <input aria-label="Search" v-model="query">
    <output data-testid="page">{{ page }}</output>
    <output data-testid="page-size">{{ pageSize }}</output>
  `
})

describe('directory route state', () => {
  it('initializes from the URL and replaces it after a debounced search', async () => {
    const wrapper = await mountSuspended(DirectoryProbe, {
      route: '/library/maps?query=wolf&page=3&pageSize=50'
    })

    expect((wrapper.get('[aria-label="Search"]').element as HTMLInputElement).value).toBe('wolf')
    expect(wrapper.get('[data-testid="page"]').text()).toBe('3')
    expect(wrapper.get('[data-testid="page-size"]').text()).toBe('50')

    await wrapper.get('[aria-label="Search"]').setValue('orc')
    await new Promise(resolve => setTimeout(resolve, 350))

    expect(wrapper.vm.page).toBe(1)
    expect(wrapper.vm.load).toHaveBeenCalled()
  })
})
