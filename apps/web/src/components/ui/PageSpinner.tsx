/**
 * Full-page spinner used by Next.js `loading.tsx` route files.
 * Provides instant visual feedback while routes compile (dev) or stream (prod).
 */
export default function PageSpinner() {
  return (
    <div className="flex items-center justify-center min-h-screen bg-gray-50/50">
      <div className="flex flex-col items-center gap-3">
        <div className="animate-spin rounded-full h-10 w-10 border-4 border-green-200 border-t-green-600" />
        <p className="text-sm text-gray-500 font-inter">Cargando…</p>
      </div>
    </div>
  );
}
