export type VocabularyEntry = {
  text: string;
  /** 保存时自动翻译（英→简中） */
  translationZh?: string;
  savedAt: string;
  source?: string;
};

export type VocabularyFile = {
  version: 1;
  entries: VocabularyEntry[];
};
