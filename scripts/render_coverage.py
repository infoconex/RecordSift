"""Render dotnet-coverage Cobertura XML as dependency-free HTML and GitHub Markdown."""

import argparse
from html import escape
from pathlib import Path
import re
import xml.etree.ElementTree as ET

STYLE = """
<style>
:root{color-scheme:light dark;--line:#d0d7de;--good:#1a7f37;--bad:#cf222e;--muted:#57606a;--link:#0969da}
@media(prefers-color-scheme:dark){:root{--line:#30363d;--good:#3fb950;--bad:#f85149;--muted:#8b949e;--link:#6cb6ff}}
*{box-sizing:border-box}body{font:14px/1.5 system-ui,-apple-system,"Segoe UI",sans-serif;max-width:1360px;padding:24px;margin:auto}
a{color:var(--link);text-decoration:none}a:hover{text-decoration:underline}h1,h2{line-height:1.25}
.muted{color:var(--muted)}.cards{display:grid;grid-template-columns:repeat(auto-fit,minmax(185px,1fr));gap:12px;margin:22px 0}
.card,details{border:1px solid var(--line);border-radius:9px;padding:12px;margin:10px 0}.card strong{display:block;font-size:27px}
.card small{color:var(--muted)}table{border-collapse:collapse;width:100%;margin:12px 0}th,td{padding:7px 10px;text-align:left;border-bottom:1px solid var(--line);vertical-align:top}
th{font-size:12px;color:var(--muted)}.num{text-align:right;white-space:nowrap;font-variant-numeric:tabular-nums}
summary{cursor:pointer;font-weight:600}.good{color:var(--good)}.bad{color:var(--bad)}
input{width:100%;max-width:460px;border:1px solid var(--line);border-radius:7px;padding:10px;background:transparent;color:inherit}
.source{font:12px/1.5 ui-monospace,SFMono-Regular,Consolas,monospace;table-layout:fixed}
.source td{padding:0 8px;border:0}.source .ln{width:55px;text-align:right;border-right:1px solid var(--line)}
.source .hits{width:140px;color:var(--muted);white-space:nowrap}.source .code{white-space:pre;overflow-x:auto}
.source .covered{background:rgba(46,160,67,.12)}.source .uncovered{background:rgba(248,81,73,.14)}
.source tr:target{outline:2px solid var(--link)}.source code{white-space:pre}
@media(max-width:600px){body{padding:12px}th,td{padding:5px}.source .hits{width:90px;font-size:10px}}
</style>
"""


def percentage(value):
    return "—" if value is None else f"{float(value) * 100:.2f}%"


def html_document(title, body):
    return ("<!doctype html><html lang='en'><head><meta charset='utf-8'>"
            "<meta name='viewport' content='width=device-width,initial-scale=1'>"
            f"<title>{escape(title)}</title>{STYLE}</head><body>{body}</body></html>")


def source_path(reported, root):
    """Resolve coverage PDB paths only underneath the checked-out src tree."""
    match = re.search(r"(?:^|[/\\])(src[/\\].*\.cs)$", reported, re.I)
    if match is None:
        raise ValueError(f"Coverage source outside src: {reported}")
    parts = match.group(1).replace("\\", "/").split("/")
    if any(part in ("", ".", "..") for part in parts):
        raise ValueError(f"Unsafe source path: {reported}")
    candidate = (root / Path(*parts)).resolve()
    if not candidate.is_relative_to((root / "src").resolve()) or not candidate.is_file():
        raise ValueError(f"Coverage source missing or outside checkout: {reported}")
    return candidate, "/".join(parts)


def render(coverage_file, output_directory, source_root):
    root = ET.parse(coverage_file).getroot()
    if root.tag != "coverage":
        raise ValueError("Expected Cobertura coverage XML")
    counts = {key: int(root.get(key, "0")) for key in
              ("lines-valid", "lines-covered", "branches-valid", "branches-covered")}
    if (counts["lines-valid"] <= 0 or counts["branches-valid"] <= 0 or
            counts["lines-covered"] > counts["lines-valid"] or
            counts["branches-covered"] > counts["branches-valid"]):
        raise ValueError("Missing or inconsistent Cobertura line/branch totals")
    packages = []
    files = {}
    methods = []
    for package in root.findall("./packages/package"):
        classes = []
        for cls in package.findall("./classes/class"):
            source, relative = source_path(cls.get("filename", ""), source_root)
            if relative not in files:
                files[relative] = dict(id=f"file-{len(files) + 1:03d}", source=source,
                                       relative=relative, lines={}, methods=[])
            file = files[relative]
            for line in cls.findall("./lines/line"):
                number = int(line.get("number", "0"))
                if number < 1:
                    raise ValueError(f"Invalid source line: {relative}")
                info = file["lines"].setdefault(number, dict(hits=0, branch=""))
                info["hits"] = max(info["hits"], int(line.get("hits", "0")))
                if line.get("branch", "").lower() == "true":
                    info["branch"] = line.get("condition-coverage", "branch")
            class_methods = []
            for method in cls.findall("./methods/method"):
                lines = method.findall("./lines/line")
                item = dict(name=method.get("name", ""), signature=method.get("signature", ""),
                            class_name=cls.get("name", ""), line=float(method.get("line-rate", "0")),
                            branch=(float(method.get("branch-rate", "0")) if any(
                                line.get("branch", "").lower() == "true" for line in lines) else None),
                            executed=any(int(x.get("hits", "0")) > 0 for x in lines),
                            first=min((int(x.get("number", "1")) for x in lines), default=1),
                            file=file)
                methods.append(item)
                class_methods.append(item)
                file["methods"].append(item)
            classes.append(dict(name=cls.get("name", ""), line=float(cls.get("line-rate", "0")),
                                branch=(float(cls.get("branch-rate", "0")) if cls.find(
                                    "./lines/line[@branch='True']") is not None else None),
                                methods=class_methods, file=file))
        packages.append(dict(name=package.get("name", ""), line=float(package.get("line-rate", "0")),
                             branch=float(package.get("branch-rate", "0")), classes=classes))
    if not packages or not files or not methods:
        raise ValueError("Missing Cobertura package, source, or method data")
    executed = sum(m["executed"] for m in methods)
    totals = [("Lines", counts["lines-covered"], counts["lines-valid"]),
              ("Branches", counts["branches-covered"], counts["branches-valid"]),
              ("Methods executed", executed, len(methods))]
    out = output_directory.resolve()
    (out / "files").mkdir(parents=True, exist_ok=True)
    body = ["<h1>RecordSift code coverage</h1>",
            "<p class='muted'>Full NUnit suite · Microsoft dotnet-coverage · Cobertura results</p>",
            "<div class='cards'>"]
    for name, covered, valid in totals:
        body.append(f"<div class='card'><small>{escape(name)}</small><strong>{percentage(covered / valid)}</strong>"
                    f"<small>{covered:,} / {valid:,}</small></div>")
    body.append("</div><p class='muted'>Method execution means at least one instrumented line was hit, "
                "not that all statements or decisions were covered.</p>")
    body.append("<h2>Assemblies</h2><table><tr><th>Assembly</th><th class='num'>Lines</th>"
                "<th class='num'>Branches</th><th class='num'>Methods run</th></tr>")
    for index, package in enumerate(packages):
        pm = [m for cls in package["classes"] for m in cls["methods"]]
        body.append(f"<tr><td><a href='#assembly-{index}'>{escape(package['name'])}</a></td>"
                    f"<td class='num'>{percentage(package['line'])}</td>"
                    f"<td class='num'>{percentage(package['branch'])}</td>"
                    f"<td class='num'>{sum(m['executed'] for m in pm)} / {len(pm)}</td></tr>")
    body.append("</table><h2>Classes and methods</h2><input id='filter' placeholder='Search classes and methods' "
                "aria-label='Filter classes and methods'>")
    for index, package in enumerate(packages):
        body.append(f"<section id='assembly-{index}'><h3>{escape(package['name'])}</h3>")
        for cls in package["classes"]:
            body.append(f"<details class='class'><summary>{escape(cls['name'])} · "
                        f"{percentage(cls['line'])} lines · {percentage(cls['branch'])} branches "
                        f"· {len(cls['methods'])} methods</summary>"
                        f"<p><a href='files/{cls['file']['id']}.html'>View highlighted source</a></p>"
                        "<table><tr><th>Method</th><th>Executed</th><th class='num'>Lines</th>"
                        "<th class='num'>Branches</th></tr>")
            for m in cls["methods"]:
                status = "<span class='good'>Yes</span>" if m["executed"] else "<span class='bad'>No</span>"
                body.append(f"<tr><td><a href='files/{m['file']['id']}.html#L{m['first']}'>"
                            f"{escape(m['name'] + m['signature'])}</a></td><td>{status}</td>"
                            f"<td class='num'>{percentage(m['line'])}</td>"
                            f"<td class='num'>{percentage(m['branch'])}</td></tr>")
            body.append("</table></details>")
        body.append("</section>")
    body.append("<p class='muted'>Coverage is diagnostic, not a percentage quality gate.</p>"
                "<script>document.getElementById('filter').addEventListener('input',e=>{"
                "const q=e.target.value.toLowerCase().trim();"
                "document.querySelectorAll('details.class').forEach(d=>{const found=d.textContent.toLowerCase().includes(q);"
                "d.hidden=!found;d.open=Boolean(q)&&found})});</script>")
    (out / "index.html").write_text(html_document("RecordSift code coverage", "".join(body)), encoding="utf-8")
    for file in files.values():
        lines = file["source"].read_text(encoding="utf-8-sig").splitlines()
        page = ["<p><a href='../index.html'>← Coverage overview</a></p>",
                f"<h1>{escape(file['relative'])}</h1>",
                "<h2>Methods</h2><table><tr><th>Method</th><th>Executed</th>"
                "<th class='num'>Lines</th><th class='num'>Branches</th></tr>"]
        for m in file["methods"]:
            label = escape(m["class_name"] + "." + m["name"] + m["signature"])
            status = "<span class='good'>Yes</span>" if m["executed"] else "<span class='bad'>No</span>"
            page.append(f"<tr><td><a href='#L{m['first']}'>{label}</a></td><td>{status}</td>"
                        f"<td class='num'>{percentage(m['line'])}</td>"
                        f"<td class='num'>{percentage(m['branch'])}</td></tr>")
        page.append("</table><h2>Source coverage</h2><p class='muted'>Green: hit; red: unexecuted; "
                    "unshaded: not instrumented. Branch labels show the collector's condition coverage.</p>"
                    "<table class='source'>")
        for number, source_line in enumerate(lines, 1):
            info = file["lines"].get(number)
            status = "" if info is None else "hit" if info["hits"] else "miss"
            css = "" if not status else "covered" if status == "hit" else "uncovered"
            branch = "" if info is None or not info["branch"] else " · " + info["branch"]
            page.append(f"<tr id='L{number}' class='{css}'><td class='ln'><a href='#L{number}'>{number}</a></td>"
                        f"<td class='hits'>{escape(status + branch)}</td>"
                        f"<td class='code'><code>{escape(source_line)}</code></td></tr>")
        page.append("</table>")
        (out / "files" / f"{file['id']}.html").write_text(
            html_document(file["relative"] + " | coverage", "".join(page)), encoding="utf-8")
    summary = ["# RecordSift code coverage", "", "| Metric | Coverage | Covered / valid |",
               "| --- | ---: | ---: |"]
    for name, covered, valid in totals:
        summary.append(f"| {name} | {percentage(covered / valid)} | {covered} / {valid} |")
    summary += ["", "## Coverage by assembly", "",
                "| Assembly | Lines | Branches | Methods run |",
                "| --- | ---: | ---: | ---: |"]
    for package in packages:
        pm = [m for cls in package["classes"] for m in cls["methods"]]
        summary.append(f"| {package['name']} | {percentage(package['line'])} | "
                       f"{percentage(package['branch'])} | {sum(m['executed'] for m in pm)} / {len(pm)} |")
    missed = [m for m in methods if not m["executed"]]
    summary += ["", f"## Unexecuted methods ({len(missed)})", ""]
    summary += ["- " + (m["class_name"] + "." + m["name"] + m["signature"]).replace("|", "\\|")
                for m in missed] or ["None."]
    summary += ["", "**Detailed source report:** Download and extract the coverage workflow artifact, "
                "then open the coverage/report/index.html file.", "",
                "Coverage is diagnostic; no minimum coverage threshold is enforced."]
    (out / "SummaryGithub.md").write_text("\n".join(summary) + "\n", encoding="utf-8")
    print(f"Report generated: {len(files)} source files, {len(methods)} methods, {executed} executed")
    print(f"Open {out / 'index.html'}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--coverage", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--source-root", type=Path, required=True)
    args = parser.parse_args()
    render(args.coverage, args.output, args.source_root.resolve())


if __name__ == "__main__":
    main()
