# CSV/Excel-import: varningstexter

Kolumnen `Varningar` visar bara sådant som användaren behöver kontrollera eller komplettera. Detaljer visas via tooltip eller popup när användaren håller musen över eller klickar på varningssymbolen.

## Namn saknas

Kort text: `Namn saknas`

Tooltip: `Raden innehåller kalkyldata men saknar namn/beskrivning.`

English tooltip: `The row contains calculation data but no name/description.`

## Enhet saknas

Kort text: `Enhet saknas`

Tooltip: `Raden innehåller mängd men ingen enhet.`

English tooltip: `The row contains quantity but no unit.`

## Mängd saknas

Kort text: `Mängd saknas`

Tooltip: `Raden innehåller enhet men ingen mängd.`

English tooltip: `The row contains a unit but no quantity.`

## Ogiltig mängd

Kort text: `Ogiltig mängd`

Tooltip: `Kvantitet innehåller ett ogiltigt numeriskt värde.`

English tooltip: `Quantity contains an invalid numeric value.`

## Komplettera raden

Kort text: `Komplettera raden`

Tooltip: `Raden behöver kompletteras eller få en annan typ.`

English tooltip: `The row needs to be completed or changed to another type.`

## Regler

- Kodtext importeras utan varning.
- 3-streckad kod och 4-streckad kod importeras utan varning.
- `#VÄRDEFEL!` i Kvantitet visas som `Ogiltig mängd`.
- `#VÄRDEFEL!` i Á-pris, Belopp eller Enhet ignoreras och importeras som tomt värde.
- Samma korta varning visas bara en gång per rad.
