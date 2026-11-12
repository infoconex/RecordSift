"""Render dotnet-coverage Cobertura XML as dependency-free HTML and GitHub Markdown."""

import argparse
from html import escape
from pathlib import Path
import re
import xml.etree.ElementTree as ET

STYLE = """
<style>
:root{color-scheme:light;--bg:#f5f7fb;--panel:#fff;--text:#162338;--muted:#607087;--line:#dfe6ef;--link:#245cce;--accent:#2563eb;--good:#15803d;--bad:#b42318;--soft-good:#e9f8ee;--soft-bad:#fef0ee;--shadow:0 12px 36px rgba(28,44,74,.055)}
@media(prefers-color-scheme:dark){:root{color-scheme:dark;--bg:#111823;--panel:#1a2635;--text:#edf2f9;--muted:#a7b6c9;--line:#35445a;--link:#8db5ff;--accent:#80a9ff;--good:#6ad78d;--bad:#ffa094;--soft-good:#213b30;--soft-bad:#472b31;--shadow:0 12px 36px rgba(0,0,0,.12)}}
*{box-sizing:border-box}html{scroll-behavior:smooth}body{font:14px/1.6 system-ui,-apple-system,"Segoe UI",sans-serif;color:var(--text);background:var(--bg);max-width:1480px;padding:28px 36px 65px;margin:auto}
a{color:var(--link);text-decoration:none}a:hover{text-decoration:underline}a:focus-visible,summary:focus-visible,input:focus-visible{outline:3px solid var(--accent);outline-offset:3px}
.topbar{display:flex;align-items:center;justify-content:space-between;gap:12px;margin-bottom:28px}.brand{font-weight:750;letter-spacing:-.02em;font-size:17px}.brand-mark{display:inline-grid;place-items:center;width:32px;height:32px;color:white;background:#255ed1;border-radius:8px;margin-right:9px;font-size:12px}.top-meta,.eyebrow{font-size:11px;font-weight:750;color:var(--muted);text-transform:uppercase;letter-spacing:.1em}
.hero{margin:0 0 28px}.hero h1{font-size:clamp(28px,4vw,39px);line-height:1.16;letter-spacing:-.035em;margin:7px 0}.hero p{color:var(--muted);margin:0;max-width:760px}
.section-heading{display:flex;justify-content:space-between;align-items:center;gap:12px;margin:30px 0 12px}.section-heading h2{font-size:20px;letter-spacing:-.02em;margin:0}.section-heading .muted{font-size:12px}
.cards{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:16px;margin:18px 0}.card{background:var(--panel);border:1px solid var(--line);border-radius:13px;padding:19px 22px;box-shadow:var(--shadow)}.card small{display:block;color:var(--muted);font-weight:650}.card strong{display:block;font-size:clamp(24px,3.3vw,34px);letter-spacing:-.04em;line-height:1.4;font-variant-numeric:tabular-nums}.card-meter{height:6px;border-radius:9px;background:var(--line);overflow:hidden;margin-top:13px}.card-fill{height:100%;background:var(--accent);border-radius:9px}
.panel{background:var(--panel);border:1px solid var(--line);border-radius:13px;box-shadow:var(--shadow);padding:17px 20px;margin:13px 0}.panel-intro{color:var(--muted);margin:3px 0 13px}.small{font-size:12px}.muted{color:var(--muted)}.note{border-left:3px solid var(--accent);padding:8px 13px;background:var(--panel);color:var(--muted);border-radius:0 7px 7px 0;margin:12px 0 22px}
table{width:100%;border-collapse:collapse;margin:7px 0}th,td{padding:10px 12px;text-align:left;border-bottom:1px solid var(--line);vertical-align:top}tr:last-child td{border-bottom:0}th{font-size:11px;color:var(--muted);font-weight:750;text-transform:uppercase;letter-spacing:.04em}tr:hover td{background:rgba(80,120,175,.045)}td.num,th.num{text-align:right;white-space:nowrap;font-variant-numeric:tabular-nums}
code,.mono{font:12px/1.5 ui-monospace,SFMono-Regular,Consolas,monospace}td code{overflow-wrap:anywhere;word-break:break-word}.method-main{display:block;font-weight:650}.method-type{display:block;color:var(--muted);font-size:12px}.chip{display:inline-block;border:1px solid var(--line);border-radius:999px;padding:2px 9px;font-size:11px;font-weight:700}.chip.bad{color:var(--bad);background:var(--soft-bad);border-color:transparent}.chip.good{color:var(--good);background:var(--soft-good);border-color:transparent}
details.class{border:1px solid var(--line);border-radius:10px;background:var(--panel);margin:10px 0;padding:0 14px;overflow:hidden}details.class>summary{cursor:pointer;list-style-position:inside;display:flex;align-items:center;gap:12px;padding:14px 2px;font-weight:700;flex-wrap:wrap}details.class>summary::before{content:"▸";color:var(--muted);font-size:13px}details.class[open]>summary::before{content:"▾"}details.class>summary::-webkit-details-marker{display:none}details.class>summary::marker{content:""}.summary-metrics{margin-left:auto;color:var(--muted);font-weight:450;font-size:12px}.class-content{padding:4px 3px 15px}
details.signature{margin-top:6px;font-weight:400;font-size:12px}details.signature>summary{cursor:pointer;color:var(--link);font-size:11px}details.signature code{display:block;padding:10px;background:var(--bg);border:1px solid var(--line);border-radius:6px;overflow-wrap:anywhere;white-space:normal}
.search{width:100%;max-width:510px;padding:11px 13px;background:var(--panel);color:var(--text);border:1px solid var(--line);border-radius:9px;margin:8px 0 14px}.search:focus{outline:2px solid var(--accent);outline-offset:1px}
.subtle-link{font-size:12px;font-weight:700}.source-header{display:flex;flex-direction:column;gap:8px;word-break:break-word}.source-wrap{overflow-x:auto;border:1px solid var(--line);border-radius:10px;background:var(--panel)}.source{font:12px/1.6 ui-monospace,SFMono-Regular,Consolas,monospace;table-layout:fixed;min-width:600px;margin:0}.source td{padding:0 10px;border:0}.source .ln{width:66px;text-align:right;border-right:1px solid var(--line);color:var(--muted)}.source .hits{width:133px;white-space:nowrap;color:var(--muted);font-size:11px}.source .code{white-space:pre;overflow-x:auto}.source tr.covered{background:var(--soft-good)}.source tr.uncovered{background:var(--soft-bad)}.source tr.partial{box-shadow:inset 3px 0 0 #d97706}.source tr:target{outline:2px solid var(--accent);outline-offset:-2px}
.source code{white-space:pre}.source-file{margin-top:40px;border-top:1px solid var(--line);padding-top:20px;content-visibility:auto;contain-intrinsic-size:auto 500px}.source-file:target{scroll-margin-top:18px}.source-file h2{font-size:19px;overflow-wrap:anywhere}
@media(max-width:800px){body{padding:16px 12px 45px}.cards{grid-template-columns:1fr}.topbar{margin-bottom:20px}.panel{padding:12px}th,td{padding:7px}.summary-metrics{margin-left:0}table{font-size:12px}.source .hits{width:110px}}
</style>
"""



def percentage(value):
    return "—" if value is None else f"{float(value) * 100:.2f}%"


def html_document(title, body):
    return ("<!doctype html><html lang='en'><head><meta charset='utf-8'>"
            "<meta name='viewport' content='width=device-width,initial-scale=1'>"
            f"<title>{escape(title)}</title>{STYLE}</head><body>{body}</body></html>")



def display_member(method):
    name = method["name"]
    if name.startswith("get_"):
        return name[4:] + " · getter"
    if name.startswith("set_"):
        return name[4:] + " · setter"
    return name


def parameter_count(signature):
    """Count top-level parameters, not commas inside C# generic type arguments."""
    if "(" not in signature or ")" not in signature:
        return 0
    params = signature.split("(", 1)[1].rsplit(")", 1)[0].strip()
    if not params:
        return 0
    depth = 0
    count = 1
    for char in params:
        if char in "<[(":
            depth += 1
        elif char in ">])":
            depth -= 1
        elif char == "," and depth == 0:
            count += 1
    return count


def compact_signature(method):
    name = display_member(method)
    if method["name"].startswith(("get_", "set_")):
        return name
    count = parameter_count(method["signature"])
    return f"{name} ({count} {'parameter' if count == 1 else 'parameters'})"


def relative_source_link(method):
    """GitHub source link; never point to an unhosted HTML file as though it were public."""
    import os
    from urllib.parse import quote
    repository = os.environ.get("GITHUB_REPOSITORY", "")
    revision = os.environ.get("GITHUB_SHA", "")
    if not re.fullmatch(r"[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+", repository):
        return ""
    if not re.fullmatch(r"[a-fA-F0-9]{40}", revision):
        return ""
    path = quote(method["file"]["relative"], safe="/")
    return f"https://github.com/{repository}/blob/{revision}/{path}#L{method['first']}"


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
                item = dict(assembly=package.get("name", ""), name=method.get("name", ""), signature=method.get("signature", ""),
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
    missed = [method for method in methods if not method["executed"]]
    body = [
        "<div id='top' class='topbar'><div class='brand'><span class='brand-mark'>RS</span>RecordSift"
        "</div><span class='top-meta'>Engineering quality / Coverage</span></div>",
        "<header class='hero'><div class='eyebrow'>Automated testing · .NET Framework 4.8</div>"
        "<h1>Coverage overview</h1><p>All five production assemblies, measured against the full NUnit suite."
        " Explore uncovered paths by assembly, class, method, and source line.</p></header>",
        "<div class='cards'>"
    ]
    for label, covered, valid in totals:
        rate = covered / valid
        body.append(
            f"<div class='card'><small>{escape(label)}</small><strong>{percentage(rate)}</strong>"
            f"<small>{covered:,} of {valid:,} measured</small>"
            f"<div class='card-meter'><div class='card-fill' style='width:{rate * 100:.2f}%'></div></div></div>"
        )
    body.append("</div><p class='note'>Coverage indicates which code executed, not whether assertions "
                "fully verify behavior. No minimum percentage is enforced.</p>")
    body.append(
        "<div class='section-heading'><h2>Assembly coverage</h2>"
        f"<span class='muted'>{len(packages)} production assemblies</span></div>"
        "<div class='panel'><table><thead><tr><th>Assembly</th><th class='num'>Lines</th>"
        "<th class='num'>Branches</th><th class='num'>Methods executed</th>"
        "</tr></thead><tbody>"
    )
    for index, package in enumerate(packages):
        package_methods = [m for cls in package["classes"] for m in cls["methods"]]
        body.append(
            f"<tr><td><a href='#assembly-{index}'>{escape(package['name'])}</a></td>"
            f"<td class='num'>{percentage(package['line'])}</td>"
            f"<td class='num'>{percentage(package['branch'])}</td>"
            f"<td class='num'>{sum(m['executed'] for m in package_methods)} / {len(package_methods)}</td></tr>"
        )
    body.append("</tbody></table></div>")
    body.append(
        "<div class='section-heading' id='unexecuted'><h2>Methods not executed</h2>"
        f"<span class='chip bad'>{len(missed)} to review</span></div>"
        "<div class='panel'><p class='panel-intro'>These methods had no instrumented lines executed. "
        "They are not stack traces. Open the source or expand a signature to investigate.</p>"
    )
    if missed:
        body.append("<table><thead><tr><th>Assembly / type</th><th>Method</th>"
                    "<th>Source</th></tr></thead><tbody>")
        for method in missed:
            short_assembly = method["assembly"].replace("RecordSift.", "")
            body.append(
                "<tr><td><span class='method-main'>" + escape(short_assembly) +
                "</span><span class='method-type'>" + escape(method["class_name"].rsplit(".", 1)[-1]) +
                "</span></td><td><span class='method-main'>" + escape(compact_signature(method)) +
                "</span><details class='signature'><summary>Full signature</summary><code>" +
                escape(method["name"] + method["signature"]) + "</code></details></td>"
                f"<td><a class='subtle-link' href='files/{method['file']['id']}.html#L{method['first']}'>"
                f"{escape(method['file']['relative'].rsplit('/', 1)[-1])}:{method['first']}</a></td></tr>"
            )
        body.append("</tbody></table>")
    else:
        body.append("<p><span class='chip good'>All measured methods were executed.</span></p>")
    body.append("</div>")
    body.append(
        "<div class='section-heading'><h2>Explore classes and methods</h2>"
        "<span class='muted'>Expand a class to inspect methods</span></div>"
        "<input class='search' id='filter' placeholder='Search by class, namespace, or method' "
        "aria-label='Filter classes and methods'>"
    )
    for index, package in enumerate(packages):
        body.append(
            f"<section id='assembly-{index}'><div class='section-heading'><h2>{escape(package['name'])}</h2>"
            f"<span class='muted'>{len(package['classes'])} classes</span></div>"
        )
        for cls in package["classes"]:
            cls_name = escape(cls["name"])
            body.append(
                "<details class='class'><summary><span>" + cls_name + "</span>"
                "<span class='summary-metrics'>" + percentage(cls["line"]) + " lines · " +
                percentage(cls["branch"]) + " branches · " +
                str(len(cls["methods"])) + " methods</span></summary><div class='class-content'>"
                f"<a class='subtle-link' href='files/{cls['file']['id']}.html'>View highlighted source</a>"
                "<table><thead><tr><th>Method</th><th>Executed</th>"
                "<th class='num'>Lines</th><th class='num'>Branches</th></tr></thead><tbody>"
            )
            for method in cls["methods"]:
                status = "<span class='chip good'>Yes</span>" if method["executed"] else "<span class='chip bad'>No</span>"
                body.append(
                    f"<tr><td><a class='method-main' href='files/{method['file']['id']}.html#L{method['first']}'>"
                    f"{escape(compact_signature(method))}</a>"
                    "<details class='signature'><summary>Full signature</summary><code>" +
                    escape(method["name"] + method["signature"]) +
                    "</code></details></td>"
                    f"<td>{status}</td><td class='num'>{percentage(method['line'])}</td>"
                    f"<td class='num'>{percentage(method['branch'])}</td></tr>"
                )
            body.append("</tbody></table></div></details>")
        body.append("</section>")
    body.append(
        "<p class='note'>Method execution requires at least one covered line; full line or branch coverage "
        "is a different metric. The report is generated entirely from the checked-out source and Cobertura XML.</p>"
        "<script>const input=document.getElementById('filter');"
        "input.addEventListener('input',()=>{const q=input.value.toLowerCase().trim();"
        "document.querySelectorAll('details.class').forEach(d=>{const hit=d.textContent.toLowerCase().includes(q);"
        "d.hidden=!hit;if(q&&hit)d.open=true;if(!q)d.open=false})});</script>"
    )
    (out / "index.html").write_text(html_document("RecordSift code coverage", "".join(body)), encoding="utf-8")
    embedded_sources = []
    for file in files.values():
        lines = file["source"].read_text(encoding="utf-8-sig").splitlines()
        page = [
            "<div class='topbar'><a class='brand' href='../index.html'>"
            "<span class='brand-mark'>RS</span>RecordSift</a>"
            "<span class='top-meta'>Source coverage</span></div>",
            "<header class='hero source-header'><div class='eyebrow'>Source file</div>"
            f"<h1>{escape(file['relative'])}</h1>"
            "<p>Coverage at a glance. Select a method to navigate to its first measured line.</p>"
            "</header><div class='panel'>",
            f"<div class='section-heading'><h2>Methods in this file</h2>"
            f"<span class='muted'>{len(file['methods'])} instrumented methods</span></div>",
            "<table><thead><tr><th>Method</th><th>Executed</th>"
            "<th class='num'>Lines</th><th class='num'>Branches</th></tr></thead><tbody>",
        ]
        for method in file["methods"]:
            label = escape(compact_signature(method))
            status = "<span class='chip good'>Yes</span>" if method["executed"] else "<span class='chip bad'>No</span>"
            page.append(
                f"<tr><td><a class='method-main' href='#L{method['first']}'>{label}</a>"
                "<details class='signature'><summary>Full signature</summary><code>" +
                escape(method["class_name"] + "." + method["name"] + method["signature"]) +
                "</code></details></td>"
                f"<td>{status}</td><td class='num'>{percentage(method['line'])}</td>"
                f"<td class='num'>{percentage(method['branch'])}</td></tr>"
            )
        page.append(
            "</tbody></table></div><div class='section-heading'><h2>Source lines</h2>"
            "<span class='muted'>Line hits and branch decisions</span></div>"
            "<p class='muted small'>Green: covered · red: not executed · amber edge: partially covered branch"
            " · unshaded: no instrumentation. Branch percentages refer to decisions on that line.</p>"
            "<div class='source-wrap'><table class='source'><tbody>"
        )
        for number, source_line in enumerate(lines, 1):
            info = file["lines"].get(number)
            status = "" if info is None else "hit" if info["hits"] else "miss"
            css = "" if not status else "covered" if status == "hit" else "uncovered"
            branch = "" if info is None or not info["branch"] else " · " + info["branch"]
            if branch:
                match = re.search(r"(\d+(?:\.\d+)?)%", branch)
                if match and float(match.group(1)) < 100:
                    css += " partial"
            page.append(
                f"<tr id='L{number}' class='{css}'><td class='ln'><a href='#L{number}'>{number}</a></td>"
                f"<td class='hits'>{escape(status + branch)}</td>"
                f"<td class='code'><code>{escape(source_line)}</code></td></tr>"
            )
        page.append("</tbody></table></div>")
        source_markup = "".join(page)
        (out / "files" / f"{file['id']}.html").write_text(
            html_document(file["relative"] + " | coverage", source_markup), encoding="utf-8"
        )
        embedded_markup = source_markup.replace("href='../index.html'", "href='#top'")
        embedded_markup = re.sub(
            r"(href='#|id=')L(\d+)'",
            lambda match: match.group(1) + file["id"] + "-L" + match.group(2) + "'",
            embedded_markup,
        )
        embedded_sources.append(
            f"<section class='source-file' id='source-{file['id']}'>" +
            embedded_markup + "</section>"
        )
    # GitHub Actions v7 supports displaying a non-zipped, single HTML file in-browser.
    # All CSS, JS and source pages are embedded; do not rely on relative file links.
    standalone = "".join(body)
    standalone = re.sub(
        r"files/(file-\d+)\.html#L(\d+)",
        lambda match: "#" + match.group(1) + "-L" + match.group(2),
        standalone,
    )
    standalone = re.sub(
        r"files/(file-\d+)\.html",
        lambda match: "#source-" + match.group(1),
        standalone,
    )
    standalone += (
        "<div class='section-heading' id='source-files'><h2>Source explorer</h2>"
        f"<span class='muted'>{len(files)} files · bundled for browser viewing</span></div>"
        + "".join(embedded_sources)
    )
    (out / "view.html").write_text(
        html_document("RecordSift coverage | Interactive report", standalone),
        encoding="utf-8",
    )
    summary = [
        "# RecordSift · Code coverage",
        "",
        "Coverage measured against the **full five-assembly NUnit suite**. "
        "Coverage is diagnostic; no minimum coverage threshold is enforced.",
        "",
        "| Metric | Coverage | Covered / measured |",
        "| :--- | ---: | ---: |",
    ]
    for label, covered, valid in totals:
        summary.append(f"| **{label}** | **{percentage(covered / valid)}** | {covered:,} / {valid:,} |")
    summary += [
        "",
        "> **How to read this:** A method is executed when at least one measured line is hit. "
        "This is not the same as full line or branch coverage.",
        "",
        "## Coverage by assembly",
        "",
        "| Assembly | Lines | Branches | Methods executed |",
        "| :--- | ---: | ---: | ---: |",
    ]
    for package in packages:
        package_methods = [m for cls in package["classes"] for m in cls["methods"]]
        short_name = package["name"].replace("RecordSift.", "")
        summary.append(
            f"| {short_name} | {percentage(package['line'])} | {percentage(package['branch'])} "
            f"| {sum(m['executed'] for m in package_methods)} / {len(package_methods)} |"
        )
    summary += [
        "",
        f"## Methods to review ({len(missed)} not executed)",
        "",
        "The names below are **method signatures, not call stacks**. "
        "Full parameter types are in the interactive HTML report.",
        "",
    ]
    if missed:
        summary += [
            "| Assembly | Class | Method | Source |",
            "| :--- | :--- | :--- | :--- |",
        ]
        for method in missed:
            short_assembly = method["assembly"].replace("RecordSift.", "")
            short_class = method["class_name"].rsplit(".", 1)[-1]
            member = compact_signature(method)
            source_file = method["file"]["relative"].rsplit("/", 1)[-1]
            reference = f"{source_file}:{method['first']}"
            source_url = relative_source_link(method)
            if source_url:
                reference = f"[{reference}]({source_url})"
            summary.append(
                f"| {short_assembly} | {short_class.replace('|', chr(92) + '|')} "
                f"| " + member.replace("|", "\\|") + f" | {reference} |"
            )
    else:
        summary.append("All measured methods were executed.")
    summary += [
        "",
        "### Detailed report",
        "",
        "The full report contains assembly, class and method navigation, searchable types, "
        "readable signatures and highlighted source lines. The workflow adds a direct "
        "browser link to the single-file HTML artifact below.",
        "",
        "For offline navigation, download the ZIP artifact, extract it, then open "
        + chr(96) + "coverage/report/index.html" + chr(96) + ".",
        "",
        "_Coverage is measured data, not proof that assertions validate behavior._",
    ]
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
