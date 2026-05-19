Central task synonym dictionary

Optional file path:
C:\ProgramData\ProjectManagement\ML\task-synonyms.csv

Format:
Synonym;CanonicalWords

Examples:
schaktningsarbete;schakt
jordarbete;schakt
gatuarbete;vag schakt
gronomrade;gron plantering
telekabel;tele kabel

Notes:
- Use one synonym per line.
- Semicolon is preferred, comma also works.
- Lines starting with # are ignored.
- Restart the application after changing the file because the dictionary is loaded once per process.
- After larger synonym changes, run Rebuild Search Index and then Train ML.
