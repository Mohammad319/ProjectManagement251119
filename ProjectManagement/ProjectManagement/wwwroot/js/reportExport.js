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

    function buildSpreadsheetML(title, columns, rows) {
        var xml = '<?xml version="1.0" encoding="UTF-8"?>\n';
        xml += '<?mso-application progid="Excel.Sheet"?>\n';
        xml += '<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet"\n';
        xml += '  xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet"\n';
        xml += '  xmlns:x="urn:schemas-microsoft-com:office:excel">\n';
        xml += '<Styles>\n';
        xml += '  <Style ss:ID="th"><Font ss:Bold="1"/><Interior ss:Color="#F1F5F9" ss:Pattern="Solid"/><Borders><Border ss:Position="Bottom" ss:LineStyle="Continuous" ss:Weight="1"/></Borders></Style>\n';
        xml += '  <Style ss:ID="sum"><Font ss:Bold="1" ss:Italic="1"/><Interior ss:Color="#F8FAFC" ss:Pattern="Solid"/></Style>\n';
        xml += '  <Style ss:ID="total"><Font ss:Bold="1"/><Interior ss:Color="#E2E8F0" ss:Pattern="Solid"/></Style>\n';
        xml += '  <Style ss:ID="even"><Interior ss:Color="#F8FAFC" ss:Pattern="Solid"/></Style>\n';
        xml += '</Styles>\n';

        var sheetName = escapeXml(String(title).substring(0, 31));
        xml += '<Worksheet ss:Name="' + sheetName + '">\n<Table>\n';

        // Header row
        xml += '<Row ss:StyleID="th">\n';
        (columns || []).forEach(function (col) {
            xml += '  <Cell ss:StyleID="th"><Data ss:Type="String">' + escapeXml(col) + '</Data></Cell>\n';
        });
        xml += '</Row>\n';

        // Data rows
        var dataIndex = 0;
        (rows || []).forEach(function (row) {
            // Detect summary rows: first column non-empty, rest mostly empty
            var cells = Array.isArray(row) ? row : [];
            var firstVal = String(cells[0] ?? '').trim();
            var isSummary = firstVal.startsWith('Summa') || firstVal === 'Totalt';
            var styleId = isSummary
                ? (firstVal === 'Totalt' ? 'total' : 'sum')
                : (dataIndex % 2 === 1 ? 'even' : '');

            if (!isSummary) dataIndex++;

            xml += '<Row' + (styleId ? ' ss:StyleID="' + styleId + '"' : '') + '>\n';
            cells.forEach(function (value) {
                var s = String(value ?? '').trim();
                if (isNumeric(s)) {
                    xml += '  <Cell' + (styleId ? ' ss:StyleID="' + styleId + '"' : '') + '><Data ss:Type="Number">' + toNumber(s) + '</Data></Cell>\n';
                } else {
                    xml += '  <Cell' + (styleId ? ' ss:StyleID="' + styleId + '"' : '') + '><Data ss:Type="String">' + escapeXml(s) + '</Data></Cell>\n';
                }
            });
            xml += '</Row>\n';
        });

        xml += '</Table>\n</Worksheet>\n</Workbook>';
        return xml;
    }

    function buildPrintHtml(title, columns, rows) {
        var header = (columns || []).map(function (col) { return '<th>' + escapeHtml(col) + '</th>'; }).join('');
        var body = (rows || []).map(function (row, i) {
            var cells = (row || []).map(function (val) { return '<td>' + escapeHtml(val) + '</td>'; }).join('');
            var firstVal = String((row || [])[0] ?? '').trim();
            var cls = firstVal === 'Totalt' ? ' class="total"'
                : firstVal.startsWith('Summa') ? ' class="sum"'
                : i % 2 === 1 ? ' class="even"' : '';
            return '<tr' + cls + '>' + cells + '</tr>';
        }).join('');

        return '<!DOCTYPE html><html><head><meta charset="utf-8"><title>' + escapeHtml(title) + '</title>' +
            '<style>' +
            'body{margin:8mm;font-family:system-ui,-apple-system,sans-serif;color:#0f172a;font-size:11px}' +
            'h1{margin:0 0 12px;font-size:16px;font-weight:700}' +
            'table{border-collapse:collapse;width:100%}' +
            'thead{display:table-header-group}' +
            'th,td{border:1px solid #e2e8f0;padding:4px 7px;text-align:left;vertical-align:top}' +
            'th{background:#f1f5f9;font-weight:700}' +
            'tr.even td{background:#f8fafc}' +
            'tr.sum td{background:#f8fafc;font-weight:600;font-style:italic}' +
            'tr.total td{background:#e2e8f0;font-weight:700}' +
            'tr{page-break-inside:avoid}' +
            '</style></head><body><h1>' + escapeHtml(title) + '</h1>' +
            '<table><thead><tr>' + header + '</tr></thead><tbody>' + body + '</tbody></table>' +
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

    window.pmReportExport = {
        excel: function (fileName, title, columns, rows) {
            var xml = buildSpreadsheetML(title, columns, rows);
            download(new Blob([xml], { type: 'application/vnd.ms-excel;charset=utf-8' }), fileName + '.xls');
        },
        print: function (title, columns, rows, pdfMode) {
            var win = window.open('', '_blank', 'width=1400,height=900');
            if (!win) return;
            win.document.write(buildPrintHtml(title + (pdfMode ? ' - PDF' : ''), columns, rows));
            win.document.close();
            win.focus();
            setTimeout(function () { win.print(); win.close(); }, 350);
        }
    };
})();
