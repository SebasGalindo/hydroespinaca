import React from 'react';
import type { NotificationChannel, NotificationLogEntry } from '@hydroespinaca/shared';
import { CHANNEL_ICONS } from '@hydroespinaca/shared';

interface HistorySectionProps {
  history: NotificationLogEntry[];
  loading: boolean;
}

function getStatusStyle(status: string): string {
  if (status === 'sent' || status === 'delivered') return 'bg-green-100 text-green-700';
  if (status === 'failed') return 'bg-red-100 text-red-700';
  return 'bg-gray-100 text-gray-600';
}

function extractEmojiAndText(entry: NotificationLogEntry) {
  const rawTitle = entry.title || entry.templateKey || 'Notificación';
  const emojiMatch = rawTitle.match(/^([\p{Emoji_Presentation}\p{Emoji}\uFE0F]+)\s*/u);
  if (emojiMatch) {
    return { icon: emojiMatch[1], titleText: rawTitle.slice(emojiMatch[0].length).trim() };
  }
  return {
    icon: CHANNEL_ICONS[entry.channel as NotificationChannel] || '📨',
    titleText: rawTitle,
  };
}

const HistorySection = React.memo(function HistorySection({ history, loading }: HistorySectionProps) {
  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden flex flex-col">
      <div className="p-4 border-b border-gray-100 shrink-0">
        <h3 className="font-semibold text-gray-800">Historial de Envíos</h3>
      </div>
      {loading && !history.length ? (
        <div className="p-6 text-center shrink-0">
          <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-green-600 mx-auto" />
        </div>
      ) : !history.length ? (
        <div className="p-6 text-center shrink-0">
          <span className="text-3xl block mb-2">📭</span>
          <p className="text-sm text-gray-500">No hay notificaciones enviadas</p>
        </div>
      ) : (
        <div className="divide-y divide-gray-100 max-h-[120vh] overflow-y-auto [&::-webkit-scrollbar]:hidden [-ms-overflow-style:none] [scrollbar-width:none]">
          {history.map((entry) => {
            const { icon, titleText } = extractEmojiAndText(entry);
            return (
              <div key={entry.id} className="p-4 flex items-center gap-4 hover:bg-gray-50/50 transition-colors">
                <span className="text-2xl shrink-0">{icon}</span>
                <div className="flex-1 min-w-0">
                  <p className="text-sm md:text-base font-medium text-gray-800 truncate">{titleText}</p>
                  <p className="text-xs md:text-sm text-gray-400 mt-0.5">
                    {entry.sentAt
                      ? new Date(entry.sentAt).toLocaleString('es-CO', { timeZone: 'America/Bogota' })
                      : 'Pendiente'}
                  </p>
                </div>
                <span
                  className={`text-xs md:text-sm px-2.5 py-1 rounded-full shrink-0 ${getStatusStyle(entry.status)}`}
                >
                  {entry.status === 'sent' ? 'Enviado' : entry.status === 'failed' ? 'Fallido' : entry.status}
                </span>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
});

export default HistorySection;
