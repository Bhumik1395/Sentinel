const fs = require("fs");
const {
    Document,
    Packer,
    Paragraph: DocxParagraph,
    TextRun: DocxTextRun,
    Table,
    TableRow,
    TableCell,
    Header,
    Footer,
    AlignmentType,
    LevelFormat,
    TableOfContents,
    HeadingLevel,
    BorderStyle,
    WidthType,
    ShadingType,
    VerticalAlign,
    PageNumber,
    PageBreak,
    TabStopType,
    TabStopPosition,
} = require("docx");

const ACCENT = "0F766E";
const CONTENT_WIDTH = 9360;

function sanitizeText(value) {
    if (typeof value !== "string") {
        return value;
    }

    return value
        .replace(/\u0007/g, "")
        .replace(/â†’/g, "->")
        .replace(/â†“/g, "↓")
        .replace(/â”œâ”€â”€/g, "|--")
        .replace(/â””â”€â”€/g, "`--")
        .replace(/âœ…/g, "Yes")
        .replace(/âŒ/g, "No")
        .replace(/â‰¤/g, "<=")
        .replace(/âš /g, "Warning:")
        .replace(/â€”/g, "-")
        .replace(/â€“/g, "-")
        .replace(/Â·/g, "·")
        .replace(/Â/g, "")
        .replace(/â€™/g, "'")
        .replace(/â€œ/g, '"')
        .replace(/â€/g, '"');
}

class TextRun extends DocxTextRun {
    constructor(options) {
        if (typeof options === "string") {
            super(sanitizeText(options));
            return;
        }

        if (options && typeof options === "object" && typeof options.text === "string") {
            super({ ...options, text: sanitizeText(options.text) });
            return;
        }

        super(options);
    }
}

const Paragraph = DocxParagraph;

function baseParagraph(children, extra = {}) {
    return new Paragraph({
        spacing: { after: 120, line: 276 },
        children,
        ...extra,
    });
}

function h1(text) {
    return new Paragraph({
        text: sanitizeText(text),
        heading: HeadingLevel.HEADING_1,
        spacing: { before: 240, after: 160 },
        thematicBreak: false,
    });
}

function h2(text) {
    return new Paragraph({
        text: sanitizeText(text),
        heading: HeadingLevel.HEADING_2,
        spacing: { before: 180, after: 120 },
    });
}

function h3(text) {
    return new Paragraph({
        text: sanitizeText(text),
        heading: HeadingLevel.HEADING_3,
        spacing: { before: 140, after: 100 },
    });
}

function p(text) {
    return baseParagraph([new TextRun({ text: sanitizeText(text), size: 22 })]);
}

function pRich(children) {
    return baseParagraph(children);
}

function bullet(text, level = 0) {
    return new Paragraph({
        text: sanitizeText(text),
        bullet: { level },
        spacing: { after: 80 },
    });
}

function numbered(text, level = 0) {
    return new Paragraph({
        text: sanitizeText(text),
        numbering: {
            reference: "sentinel-numbering",
            level,
        },
        spacing: { after: 80 },
    });
}

function quote(text) {
    return new Paragraph({
        children: [new TextRun({ text: sanitizeText(text), italics: true, color: "555555" })],
        border: {
            left: { style: BorderStyle.SINGLE, color: ACCENT, size: 12, space: 8 },
        },
        indent: { left: 360 },
        spacing: { after: 120 },
    });
}

function hr() {
    return new Paragraph({
        border: {
            bottom: { style: BorderStyle.SINGLE, color: "D1D5DB", size: 6, space: 1 },
        },
        spacing: { after: 120 },
    });
}

function codeBlock(lines) {
    return new Paragraph({
        children: [new TextRun({ text: sanitizeText(lines.join("\n")), font: "Consolas", size: 20 })],
        shading: { type: ShadingType.CLEAR, color: "auto", fill: "F3F4F6" },
        border: {
            top: { style: BorderStyle.SINGLE, color: "D1D5DB", size: 4 },
            bottom: { style: BorderStyle.SINGLE, color: "D1D5DB", size: 4 },
            left: { style: BorderStyle.SINGLE, color: "D1D5DB", size: 4 },
            right: { style: BorderStyle.SINGLE, color: "D1D5DB", size: 4 },
        },
        spacing: { after: 120 },
    });
}

function spacer(after = 120) {
    return new Paragraph({ spacing: { after } });
}

function cellParagraph(value, bold = false) {
    return new Paragraph({
        children: [new TextRun({ text: sanitizeText(String(value)), bold, size: 20 })],
        spacing: { after: 40 },
    });
}

function buildTable(headers, rows, widths = []) {
    return new Table({
        width: { size: 100, type: WidthType.PERCENTAGE },
        rows: [
            new TableRow({
                tableHeader: true,
                children: headers.map((header, index) =>
                    new TableCell({
                        width: widths[index] ? { size: widths[index], type: WidthType.DXA } : undefined,
                        shading: { type: ShadingType.CLEAR, color: "auto", fill: "E5F3F1" },
                        children: [cellParagraph(header, true)],
                    })
                ),
            }),
            ...rows.map((row) =>
                new TableRow({
                    children: row.map((value, index) =>
                        new TableCell({
                            width: widths[index] ? { size: widths[index], type: WidthType.DXA } : undefined,
                            children: [cellParagraph(value)],
                        })
                    ),
                })
            ),
        ],
    });
}

module.exports = {
    fs,
    Document,
    Packer,
    Paragraph,
    TextRun,
    Table,
    TableRow,
    TableCell,
    Header,
    Footer,
    AlignmentType,
    LevelFormat,
    TableOfContents,
    HeadingLevel,
    BorderStyle,
    WidthType,
    ShadingType,
    VerticalAlign,
    PageNumber,
    PageBreak,
    TabStopType,
    TabStopPosition,
    CONTENT_WIDTH,
    h1,
    h2,
    h3,
    p,
    pRich,
    bullet,
    numbered,
    quote,
    hr,
    codeBlock,
    spacer,
    buildTable,
    ACCENT,
};
