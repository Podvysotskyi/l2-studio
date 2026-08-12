export function storageUploadPath(
  destination: string,
  fileName: string,
  relativePath?: string
) {
  let uploadPath = fileName
  if (relativePath) {
    const topFolderEnd = relativePath.indexOf('/')
    uploadPath = topFolderEnd >= 0
      ? relativePath.slice(topFolderEnd + 1)
      : relativePath
  }
  if (!uploadPath) uploadPath = fileName
  return destination ? `${destination}/${uploadPath}` : uploadPath
}
