(function () {
    function escapeHtml(value) {
        return String(value ?? '')
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#39;');
    }

    function escapeXml(value) {
        return String(value ?? '')
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&apos;');
    }

    function isNumeric(value) {
        var s = String(value ?? '').trim().replace(/\s/g, '');
        return s !== '' && !isNaN(s.replace(/,/g, '.'));
    }

    function toNumber(value) {
        return parseFloat(String(value ?? '').trim().replace(/\s/g, '').replace(/,/g, '.'));
    }

    function todayIso() {
        var d = new Date();
        return d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0');
    }

    // ── Minimal ZIP writer (STORE, no compression) ─────────────────────

    var CRC_TABLE = (function () {
        var t = new Uint32Array(256);
        for (var n = 0; n < 256; n++) {
            var c = n;
            for (var k = 0; k < 8; k++) c = (c & 1) ? (0xEDB88320 ^ (c >>> 1)) : (c >>> 1);
            t[n] = c >>> 0;
        }
        return t;
    })();

    function crc32(bytes) {
        var c = 0xFFFFFFFF;
        for (var i = 0; i < bytes.length; i++) c = CRC_TABLE[(c ^ bytes[i]) & 0xFF] ^ (c >>> 8);
        return (c ^ 0xFFFFFFFF) >>> 0;
    }

    function buildZip(files) {
        var enc = new TextEncoder();
        var d = new Date();
        var dosTime = ((d.getHours() & 31) << 11) | ((d.getMinutes() & 63) << 5) | ((d.getSeconds() >> 1) & 31);
        var dosDate = (((d.getFullYear() - 1980) & 127) << 9) | (((d.getMonth() + 1) & 15) << 5) | (d.getDate() & 31);
        var parts = [], centrals = [], offset = 0;

        files.forEach(function (f) {
            var nameBytes = enc.encode(f.name);
            var data = enc.encode(f.content);
            var crc = crc32(data);

            var local = new Uint8Array(30 + nameBytes.length);
            var lv = new DataView(local.buffer);
            lv.setUint32(0, 0x04034b50, true);
            lv.setUint16(4, 20, true);
            lv.setUint16(6, 0x0800, true);
            lv.setUint16(8, 0, true);
            lv.setUint16(10, dosTime, true);
            lv.setUint16(12, dosDate, true);
            lv.setUint32(14, crc, true);
            lv.setUint32(18, data.length, true);
            lv.setUint32(22, data.length, true);
            lv.setUint16(26, nameBytes.length, true);
            lv.setUint16(28, 0, true);
            local.set(nameBytes, 30);
            parts.push(local, data);

            var central = new Uint8Array(46 + nameBytes.length);
            var cv = new DataView(central.buffer);
            cv.setUint32(0, 0x02014b50, true);
            cv.setUint16(4, 20, true);
            cv.setUint16(6, 20, true);
            cv.setUint16(8, 0x0800, true);
            cv.setUint16(10, 0, true);
            cv.setUint16(12, dosTime, true);
            cv.setUint16(14, dosDate, true);
            cv.setUint32(16, crc, true);
            cv.setUint32(20, data.length, true);
            cv.setUint32(24, data.length, true);
            cv.setUint16(28, nameBytes.length, true);
            cv.setUint32(42, offset, true);
            central.set(nameBytes, 46);
            centrals.push(central);

            offset += local.length + data.length;
        });

        var centralSize = 0;
        centrals.forEach(function (c) { parts.push(c); centralSize += c.length; });

        var eocd = new Uint8Array(22);
        var ev = new DataView(eocd.buffer);
        ev.setUint32(0, 0x06054b50, true);
        ev.setUint16(8, files.length, true);
        ev.setUint16(10, files.length, true);
        ev.setUint32(12, centralSize, true);
        ev.setUint32(16, offset, true);
        parts.push(eocd);

        return new Blob(parts, { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
    }

    // ── XLSX workbook ──────────────────────────────────────────────────

    // Cell style ids (cellXfs index in styles.xml)
    var XS = { DEFAULT: 0, TITLE: 1, META: 2, SECTION_HEAD: 3, SECTION_LINE: 4, KPI_LABEL: 5, COL_HEAD: 6, EVEN: 7, SUM: 8, TOTAL: 9, MONEY: 10, PERCENT: 11, INTEGER: 12 };

    var STYLES_XML =
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>' +
        '<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">' +
        '<numFmts count="1"><numFmt numFmtId="164" formatCode="#,##0"/></numFmts>' +
        '<fonts count="5">' +
        '<font><sz val="11"/><name val="Calibri"/></font>' +
        '<font><b/><sz val="11"/><name val="Calibri"/></font>' +
        '<font><b/><sz val="14"/><name val="Calibri"/></font>' +
        '<font><b/><i/><sz val="11"/><name val="Calibri"/></font>' +
        '<font><sz val="10"/><color rgb="FF64748B"/><name val="Calibri"/></font>' +
        '</fonts>' +
        '<fills count="6">' +
        '<fill><patternFill patternType="none"/></fill>' +
        '<fill><patternFill patternType="gray125"/></fill>' +
        '<fill><patternFill patternType="solid"><fgColor rgb="FFF1F5F9"/><bgColor indexed="64"/></patternFill></fill>' +
        '<fill><patternFill patternType="solid"><fgColor rgb="FFF8FAFC"/><bgColor indexed="64"/></patternFill></fill>' +
        '<fill><patternFill patternType="solid"><fgColor rgb="FFE2E8F0"/><bgColor indexed="64"/></patternFill></fill>' +
        '<fill><patternFill patternType="solid"><fgColor rgb="FFF0F9FF"/><bgColor indexed="64"/></patternFill></fill>' +
        '</fills>' +
        '<borders count="2">' +
        '<border><left/><right/><top/><bottom/><diagonal/></border>' +
        '<border><left/><right/><top/><bottom style="thin"><color rgb="FFCBD5E1"/></bottom><diagonal/></border>' +
        '</borders>' +
        '<cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>' +
        '<cellXfs count="13">' +
        '<xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>' +
        '<xf numFmtId="0" fontId="2" fillId="0" borderId="0" xfId="0" applyFont="1"/>' +
        '<xf numFmtId="0" fontId="4" fillId="0" borderId="0" xfId="0" applyFont="1"/>' +
        '<xf numFmtId="0" fontId="1" fillId="5" borderId="0" xfId="0" applyFont="1" applyFill="1"/>' +
        '<xf numFmtId="0" fontId="0" fillId="5" borderId="0" xfId="0" applyFill="1"/>' +
        '<xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/>' +
        '<xf numFmtId="0" fontId="1" fillId="2" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1"/>' +
        '<xf numFmtId="0" fontId="0" fillId="3" borderId="0" xfId="0" applyFill="1"/>' +
        '<xf numFmtId="0" fontId="3" fillId="3" borderId="0" xfId="0" applyFont="1" applyFill="1"/>' +
        '<xf numFmtId="0" fontId="1" fillId="4" borderId="0" xfId="0" applyFont="1" applyFill="1"/>' +
        '<xf numFmtId="164" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>' +
        '<xf numFmtId="10" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>' +
        '<xf numFmtId="1" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>' +
        '</cellXfs>' +
        '</styleSheet>';

    function colRef(index) {
        var s = '', i = index + 1;
        while (i > 0) {
            var m = (i - 1) % 26;
            s = String.fromCharCode(65 + m) + s;
            i = Math.floor((i - 1) / 26);
        }
        return s;
    }

    function sheetNameSafe(title) {
        return String(title || 'Rapport').replace(/[\\\/\?\*\[\]:]/g, ' ').substring(0, 31).trim() || 'Rapport';
    }

    // grid: array of { cells: [{ v, s }] }; numeric detection happens here
    function formulaText(formula, currentRow, dataStartRow) {
        return String(formula || '')
            .replace(/^=/, '')
            .replace(/\{row\}/g, String(currentRow))
            .replace(/\{row:(\d+)\}/g, function (_, index) { return String(dataStartRow + Number(index)); });
    }

    function cellStyle(cell, fallback) {
        if (cell.numberFormat === 'money') return XS.MONEY;
        if (cell.numberFormat === 'percent') return XS.PERCENT;
        if (cell.numberFormat === 'integer') return XS.INTEGER;
        return cell.s ?? fallback ?? XS.DEFAULT;
    }

    function exportCell(value, fallbackStyle) {
        if (value && typeof value === 'object' && !Array.isArray(value) &&
            ('value' in value || 'formula' in value || 'numberFormat' in value)) {
            return { v: value.value, f: value.formula, numberFormat: value.numberFormat, s: cellStyle(value, fallbackStyle) };
        }
        return { v: value, s: fallbackStyle };
    }

    function buildSheetXml(grid, columnCount, freezeAtRow, dataStartRow) {
        // Approximate column widths from longest content (table area dominates)
        var widths = [];
        grid.forEach(function (row) {
            row.cells.forEach(function (cell, ci) {
                var len = String(cell.v ?? '').length;
                // Long single-cell info lines (title/urval) should not stretch column A
                if (row.cells.length <= 2 && len > 40) len = 40;
                if (!widths[ci] || len > widths[ci]) widths[ci] = len;
            });
        });

        var colsXml = '';
        for (var c = 0; c < Math.max(columnCount, widths.length); c++) {
            var w = Math.min(Math.max((widths[c] || 0) + 2, 8), 50);
            colsXml += '<col min="' + (c + 1) + '" max="' + (c + 1) + '" width="' + w + '" customWidth="1"/>';
        }

        var rowsXml = '';
        grid.forEach(function (row, ri) {
            var r = ri + 1;
            if (row.cells.length === 0) { rowsXml += '<row r="' + r + '"/>'; return; }
            rowsXml += '<row r="' + r + '">';
            row.cells.forEach(function (cell, ci) {
                var ref = colRef(ci) + r;
                var s = cell.s ? ' s="' + cell.s + '"' : '';
                var raw = String(cell.v ?? '');
                if (cell.f) {
                    var cached = isNumeric(raw) ? '<v>' + toNumber(raw) + '</v>' : '';
                    rowsXml += '<c r="' + ref + '"' + s + '><f>' + escapeXml(formulaText(cell.f, r, dataStartRow)) + '</f>' + cached + '</c>';
                } else if (cell.forceText !== true && isNumeric(raw)) {
                    rowsXml += '<c r="' + ref + '"' + s + '><v>' + toNumber(raw) + '</v></c>';
                } else if (raw !== '') {
                    rowsXml += '<c r="' + ref + '" t="inlineStr"' + s + '><is><t xml:space="preserve">' + escapeXml(raw) + '</t></is></c>';
                } else if (cell.s) {
                    rowsXml += '<c r="' + ref + '"' + s + '/>';
                }
            });
            rowsXml += '</row>';
        });

        var pane = freezeAtRow > 0
            ? '<pane ySplit="' + freezeAtRow + '" topLeftCell="A' + (freezeAtRow + 1) + '" activePane="bottomLeft" state="frozen"/>'
            : '';

        return '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>' +
            '<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">' +
            '<sheetViews><sheetView workbookViewId="0" showGridLines="1">' + pane + '</sheetView></sheetViews>' +
            '<cols>' + colsXml + '</cols>' +
            '<sheetData>' + rowsXml + '</sheetData>' +
            '</worksheet>';
    }

    function buildXlsx(header, columns, rows) {
        var grid = [];
        var date = header.date || todayIso();

        grid.push({ cells: [{ v: 'Rapport: ' + (header.title || ''), s: XS.TITLE, forceText: true }] });
        grid.push({ cells: [{ v: 'Skapad: ' + date, s: XS.META, forceText: true }] });

        (header.sections || []).forEach(function (section) {
            grid.push({ cells: [{ v: section.heading, s: XS.SECTION_HEAD, forceText: true }] });
            (section.lines || []).forEach(function (line) {
                grid.push({ cells: [{ v: line, s: XS.SECTION_LINE, forceText: true }] });
            });
        });

        (header.kpis || []).forEach(function (kpi) {
            grid.push({ cells: [{ v: kpi.label + ':', s: XS.KPI_LABEL, forceText: true }, exportCell(kpi, XS.DEFAULT)] });
        });

        grid.push({ cells: [] });

        var freezeAtRow = grid.length + 1; // freeze just below the single column header row
        grid.push({ cells: (columns || []).map(function (col) { return { v: col, s: XS.COL_HEAD, forceText: true }; }) });

        var dataIndex = 0;
        (rows || []).forEach(function (row) {
            var cells = Array.isArray(row) ? row : [];
            var firstVal = String(cells[0] ?? '').trim();
            var isSummary = firstVal.startsWith('Summa') || firstVal === 'Totalt';
            var styleId = isSummary
                ? (firstVal === 'Totalt' ? XS.TOTAL : XS.SUM)
                : (dataIndex % 2 === 1 ? XS.EVEN : XS.DEFAULT);
            if (!isSummary) dataIndex++;
            grid.push({ cells: cells.map(function (value) { return exportCell(value, styleId); }) });
        });

        var sheetXml = buildSheetXml(grid, (columns || []).length, freezeAtRow, freezeAtRow + 1);
        var name = sheetNameSafe(header.title);

        return buildZip([
            {
                name: '[Content_Types].xml',
                content: '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>' +
                    '<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">' +
                    '<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>' +
                    '<Default Extension="xml" ContentType="application/xml"/>' +
                    '<Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>' +
                    '<Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>' +
                    '<Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>' +
                    '</Types>'
            },
            {
                name: '_rels/.rels',
                content: '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>' +
                    '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">' +
                    '<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>' +
                    '</Relationships>'
            },
            {
                name: 'xl/workbook.xml',
                content: '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>' +
                    '<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">' +
                    '<sheets><sheet name="' + escapeXml(name) + '" sheetId="1" r:id="rId1"/></sheets>' +
                    '<calcPr calcId="191029" fullCalcOnLoad="1" forceFullCalc="1"/>' +
                    '</workbook>'
            },
            {
                name: 'xl/_rels/workbook.xml.rels',
                content: '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>' +
                    '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">' +
                    '<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>' +
                    '<Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>' +
                    '</Relationships>'
            },
            { name: 'xl/styles.xml', content: STYLES_XML },
            { name: 'xl/worksheets/sheet1.xml', content: sheetXml }
        ]);
    }

    // ── Print / PDF ────────────────────────────────────────────────────

    function buildPrintHtml(header, columns, rows, pdfMode) {
        var date = header.date || todayIso();

        var sectionsHtml = (header.sections || []).map(function (section) {
            return '<div class="sec"><div class="sec-h">' + escapeHtml(section.heading) + '</div>' +
                (section.lines || []).map(function (line) { return '<div>' + escapeHtml(line) + '</div>'; }).join('') +
                '</div>';
        }).join('');

        var kpiHtml = (header.kpis || []).map(function (kpi) {
            return '<div class="kpi"><div class="kpi-l">' + escapeHtml(kpi.label) + '</div><div class="kpi-v">' + escapeHtml(kpi.value) + '</div></div>';
        }).join('');

        var head = (columns || []).map(function (col) { return '<th>' + escapeHtml(col) + '</th>'; }).join('');
        var body = (rows || []).map(function (row, i) {
            var cells = (row || []).map(function (val) {
                var display = val && typeof val === 'object' && !Array.isArray(val) && 'value' in val ? val.value : val;
                return '<td>' + escapeHtml(display) + '</td>';
            }).join('');
            var firstVal = String((row || [])[0] ?? '').trim();
            var cls = firstVal === 'Totalt' ? ' class="total"'
                : firstVal.startsWith('Summa') ? ' class="sum"'
                : i % 2 === 1 ? ' class="even"' : '';
            return '<tr' + cls + '>' + cells + '</tr>';
        }).join('');

        return '<!DOCTYPE html><html><head><meta charset="utf-8"><title>' + escapeHtml(header.title + (pdfMode ? ' - PDF' : '')) + '</title>' +
            '<style>' +
            'body{margin:8mm;font-family:system-ui,-apple-system,sans-serif;color:#0f172a;font-size:11px}' +
            'h1{margin:0 0 2px;font-size:16px;font-weight:700}' +
            '.meta{margin:0 0 8px;color:#64748b;font-size:10px}' +
            '.info{background:#f0f9ff;border:1px solid #bae6fd;border-radius:6px;padding:7px 10px;margin:0 0 10px;font-size:10.5px}' +
            '.sec{margin:0 0 6px}.sec:last-child{margin-bottom:0}' +
            '.sec-h{font-weight:700;margin-bottom:1px}' +
            '.kpis{display:flex;gap:8px;flex-wrap:wrap;margin:0 0 10px}' +
            '.kpi{border:1px solid #e2e8f0;border-radius:6px;padding:5px 10px;min-width:70px}' +
            '.kpi-l{font-size:9px;color:#64748b}' +
            '.kpi-v{font-size:13px;font-weight:700}' +
            'table{border-collapse:collapse;width:100%}' +
            'thead{display:table-header-group}' +
            'th,td{border:1px solid #e2e8f0;padding:4px 7px;text-align:left;vertical-align:top}' +
            'th{background:#f1f5f9;font-weight:700}' +
            'tr.even td{background:#f8fafc}' +
            'tr.sum td{background:#f8fafc;font-weight:600;font-style:italic}' +
            'tr.total td{background:#e2e8f0;font-weight:700}' +
            'tr{page-break-inside:avoid}' +
            '</style></head><body>' +
            '<h1>Rapport: ' + escapeHtml(header.title) + '</h1>' +
            '<div class="meta">Skapad: ' + escapeHtml(date) + '</div>' +
            (sectionsHtml ? '<div class="info">' + sectionsHtml + '</div>' : '') +
            (kpiHtml ? '<div class="kpis">' + kpiHtml + '</div>' : '') +
            '<table><thead><tr>' + head + '</tr></thead><tbody>' + body + '</tbody></table>' +
            '</body></html>';
    }

    function download(blob, fileName) {
        var url = URL.createObjectURL(blob);
        var link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        link.remove();
        setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
    }

    function normalizeHeader(header) {
        if (typeof header === 'string') return { title: header, date: todayIso(), sections: [], kpis: [] };
        return {
            title: header?.title ?? '',
            date: header?.date || todayIso(),
            sections: header?.sections || [],
            kpis: header?.kpis || []
        };
    }

    window.pmReportExport = {
        excel: function (fileName, header, columns, rows) {
            var h = normalizeHeader(header);
            download(buildXlsx(h, columns, rows), fileName + '-' + h.date + '.xlsx');
        },
        print: function (header, columns, rows, pdfMode) {
            var win = window.open('', '_blank', 'width=1400,height=900');
            if (!win) return;
            win.document.write(buildPrintHtml(normalizeHeader(header), columns, rows, pdfMode));
            win.document.close();
            win.focus();
            setTimeout(function () { win.print(); win.close(); }, 350);
        }
    };
})();
