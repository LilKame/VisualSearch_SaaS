// CameraArea.tsx
import { Camera } from "lucide-react"
import { useRef } from "react"

interface CameraAreaProps {
  onImageCaptured: (imageUrl: string) => Promise<void> | void
}

const CameraArea = ({ onImageCaptured }: CameraAreaProps) => {
  const inputRef = useRef<HTMLInputElement>(null)

  function generateImageUrl(file: File): string {
    return URL.createObjectURL(file)
  }

  function openCamera() {
    inputRef.current?.click()
  }

  // Função que simula o tempo de resposta da API
  function delay(ms: number): Promise<void> {
    return new Promise((resolve) => {
      setTimeout(resolve, ms)
    })
  }

  async function handleFileChange(
    event: React.ChangeEvent<HTMLInputElement>
  ) {
    const file = event.target.files?.[0]

    if (file) {
      const imageUrl = generateImageUrl(file)

      // Exibe a imagem imediatamente no App
      // (por exemplo, para preview)
      await onImageCaptured(imageUrl)

      // Simula o tempo da API
      // Durante esse período, seu App pode exibir o Skeleton
      await delay(2500)

      // Aqui futuramente você fará:
      // const result = await api.sendImage(file)
      // setProduct(result)

      console.log("Resposta da API recebida")
    }

    // Limpa o input para permitir selecionar a mesma imagem novamente
    event.target.value = ""
  }

  return (
    <>
      <input
        ref={inputRef}
        type="file"
        accept="image/*"
        capture="environment"
        className="hidden"
        onChange={handleFileChange}
      />

      <div
        onClick={openCamera}
        className="
          flex flex-col items-center justify-center gap-2
          w-full h-80
          rounded-xl border-2 border-dashed
          border-muted-foreground/25
          bg-muted/30
          hover:bg-muted/50
          hover:border-primary/50
          transition-all duration-200
          cursor-pointer
        "
      >
        <Camera className="w-10 h-10 text-muted-foreground" />
        <p className="text-sm text-muted-foreground">
          Clique para abrir a imagem
        </p>
      </div>
    </>
  )
}

export default CameraArea