// ProductHistory.tsx
import { FileImage } from "lucide-react";

interface ProductHistoryProps {
  products: string[];
}

const MAX_ITEMS = 4;

const ProductHistory = ({ products }: ProductHistoryProps) => {
  return (
    <div className="grid grid-cols-2 gap-2">
      {Array.from({ length: MAX_ITEMS }).map((_, index) => {
        const image = products[index];

        return (
          <div
            key={index}
            className="
              aspect-square
              rounded-xl
              border-2 border-dashed
              border-muted-foreground/25
              bg-muted/30
              overflow-hidden
              flex items-center justify-center
              cursor-pointer
            "
          >
            {image ? (
              <img
                src={image}
                alt={`Produto ${index + 1}`}
                className="w-full h-full object-cover"
              />
            ) : (
              <div className="flex flex-col items-center gap-2 p-4 text-center">
                <FileImage className="w-8 h-8 text-muted-foreground opacity-25" />
                <p className="text-xs font-medium text-muted-foreground opacity-35">
                  Nenhum produto localizado
                </p>
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
};

export default ProductHistory;