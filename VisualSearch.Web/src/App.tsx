// App.tsx
import { useState } from "react";
import Layout from "./components/Layout";
import CameraArea from "./components/CameraArea";
import ProductHistory from "./components/ProductHistory";
import ProductResultCard from "./components/ProductResultCard";
import { Separator } from "@/components/ui/separator";

function App() {
  // Histórico das imagens capturadas
  const [products, setProducts] = useState<string[]>([]);

  // Controla se o card de resultado deve ser exibido
  const [showResult, setShowResult] = useState(false);

  /**
   * Executado quando uma nova imagem é capturada.
   * - Atualiza o histórico.
   * - Remove o card anterior.
   * - Exibe um novo ProductResultCard.
   *
   * Como o ProductResultCard já possui loading interno
   * (skeleton + delay + mock da API),
   * basta renderizá-lo novamente.
   */
  function AddProduct(imageUrl: string) {
    // Atualiza o histórico
    setProducts((prev) => {
      const filtered = prev.filter((item) => item !== imageUrl);
      const updated = [imageUrl, ...filtered];
      return updated.slice(0, 4);
    });

    // Remove o card atual
    setShowResult(false);

    // Reexibe o card no próximo ciclo de renderização,
    // forçando o useEffect interno do ProductResultCard
    // a executar novamente.
    setTimeout(() => {
      setShowResult(true);
    }, 0);
  }

  return (
    <Layout title="Busca Visual">
      {/* Área de captura da imagem */}
      <CameraArea onImageCaptured={AddProduct} />

      {/* Resultado da API */}
      <div className="mt-6 w-full max-w-sm">
        {showResult && <ProductResultCard />}
      </div>

      {/* Histórico */}
      <div className="flex max-w-sm flex-col gap-4 pt-10 text-sm">
        <div className="flex flex-col gap-1.5">
          <div className="leading-none font-bold">Histórico</div>
        </div>

        <Separator />

        <ProductHistory products={products} />
      </div>
    </Layout>
  );
}

export default App;