import { describe, expect, it } from 'vitest'
import {
  isJsonTreeBranch,
  jsonTreeBranchLabel,
  jsonTreeEntries,
  jsonTreePrimitiveLabel
} from '../../app/utils/json-tree'

describe('JSON tree helpers', () => {
  it('describes objects and arrays as expandable branches', () => {
    expect(isJsonTreeBranch({ summary: null })).toBe(true)
    expect(isJsonTreeBranch(['first', 'second'])).toBe(true)
    expect(jsonTreeBranchLabel({ summary: null })).toBe('Object(1)')
    expect(jsonTreeBranchLabel(['first', 'second'])).toBe('Array(2)')
    expect(jsonTreeEntries(['first', 'second'])).toEqual([
      ['0', 'first'],
      ['1', 'second']
    ])
  })

  it('formats JSON primitive values', () => {
    expect(isJsonTreeBranch(null)).toBe(false)
    expect(jsonTreePrimitiveLabel('Aden')).toBe('"Aden"')
    expect(jsonTreePrimitiveLabel(17)).toBe('17')
    expect(jsonTreePrimitiveLabel(false)).toBe('false')
    expect(jsonTreePrimitiveLabel(null)).toBe('null')
  })
})
