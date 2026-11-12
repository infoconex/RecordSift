"""Behavior, reproducibility and safety tests for the coverage reporter."""

import tempfile
import unittest
from unittest.mock import patch
from pathlib import Path
from xml.etree import ElementTree as ET

from render_coverage import compact_signature, parameter_count, render, source_path


def xml_fixture(filename):
    root = ET.Element("coverage", {
        "lines-valid": "2", "lines-covered": "1", "branches-valid": "2", "branches-covered": "1"})
    packages = ET.SubElement(root, "packages")
    package = ET.SubElement(packages, "package", {"name": "Demo", "line-rate": ".5", "branch-rate": ".5"})
    classes = ET.SubElement(package, "classes")
    cls = ET.SubElement(classes, "class", {"name": "Demo.Foo", "filename": filename,
                                           "line-rate": ".5", "branch-rate": ".5"})
    methods = ET.SubElement(cls, "methods")
    for name, hits, number in (("Run", 1, 1), ("Miss", 0, 2)):
        method = ET.SubElement(methods, "method", {"name": name, "signature": "()",
                                                  "line-rate": str(hits), "branch-rate": "1"})
        line_nodes = ET.SubElement(method, "lines")
        ET.SubElement(line_nodes, "line", {"number": str(number), "hits": str(hits), "branch": "False"})
    lines = ET.SubElement(cls, "lines")
    ET.SubElement(lines, "line", {"number": "1", "hits": "1", "branch": "True",
                                  "condition-coverage": "50% (1/2)"})
    ET.SubElement(lines, "line", {"number": "2", "hits": "0", "branch": "False"})
    return ET.tostring(root, encoding="unicode")


class CoverageReportTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        (self.root / "src" / "Demo").mkdir(parents=True)
        (self.root / "src" / "Demo" / "Foo.cs").write_text('<script>alert("x")</script>\nmissed\n')
        self.coverage = self.root / "coverage.xml"
        self.coverage.write_text(xml_fixture(r"C:\repo\src\Demo\Foo.cs"))
        self.output = self.root / "results"

    def test_summary_and_source_html(self):
        render(self.coverage, self.output, self.root)
        summary = (self.output / "SummaryGithub.md").read_text(encoding="utf-8")
        self.assertIn("Methods executed | 50.00% | 1 / 2", summary)
        self.assertIn("| Demo | Foo | Miss (0 parameters) | Foo.cs:2 |", summary)
        index = (self.output / "index.html").read_text(encoding="utf-8")
        self.assertIn("files/file-001.html", index)
        self.assertIn("—", index)  # No method branch data; do not advertise 100%.
        source = (self.output / "files" / "file-001.html").read_text(encoding="utf-8")
        self.assertIn("&lt;script&gt;alert", source)
        self.assertNotIn('<script>alert("x")</script>', source)
        self.assertIn("50% (1/2)", source)
        standalone = (self.output / "view.html").read_text(encoding="utf-8")
        self.assertIn("id='source-file-001'", standalone)
        self.assertIn("href='#file-001-L1'", standalone)
        self.assertIn("id='file-001-L1'", standalone)
        self.assertNotIn("href='files/file-001.html'", standalone)
        self.assertIn("&lt;script&gt;alert", standalone)
        self.assertNotIn('<script>alert("x")</script>', standalone)


    def test_concise_signature_nested_generic_parameters(self):
        signature = "(System.Func<System.Collections.Generic.IReadOnlyDictionary<string, string>, T>, bool, string)"
        self.assertEqual(3, parameter_count(signature))
        self.assertEqual(
            "Process<T> (3 parameters)",
            compact_signature({"name": "Process<T>", "signature": signature}),
        )
        self.assertEqual(
            "ReadableStoppedSummary · getter",
            compact_signature({"name": "get_ReadableStoppedSummary", "signature": "()"}),
        )

    def test_summary_links_to_repository_source_when_running_in_github(self):
        with patch.dict("os.environ", {
            "GITHUB_REPOSITORY": "infoconex/RecordSift",
            "GITHUB_SHA": "0" * 40
        }):
            render(self.coverage, self.output, self.root)
        summary = (self.output / "SummaryGithub.md").read_text(encoding="utf-8")
        self.assertIn(
            "https://github.com/infoconex/RecordSift/blob/"
            + "0" * 40 + "/src/Demo/Foo.cs#L2",
            summary,
        )

    def test_rejects_traversal(self):
        self.coverage.write_text(xml_fixture(r"C:\repo\src\..\outside.cs"))
        with self.assertRaisesRegex(ValueError, "Unsafe source path"):
            render(self.coverage, self.output, self.root)

    def test_rejects_non_source_path(self):
        with self.assertRaisesRegex(ValueError, "outside src"):
            source_path(r"C:\repo\tests\test.cs", self.root)

    def test_fails_if_branch_data_is_missing(self):
        contents = self.coverage.read_text(encoding="utf-8").replace('branches-valid="2"', 'branches-valid="0"')
        self.coverage.write_text(contents)
        with self.assertRaisesRegex(ValueError, "line/branch totals"):
            render(self.coverage, self.output, self.root)

    def test_missing_source_fails_not_silent(self):
        (self.root / "src" / "Demo" / "Foo.cs").unlink()
        with self.assertRaisesRegex(ValueError, "missing or outside checkout"):
            render(self.coverage, self.output, self.root)


if __name__ == "__main__":
    unittest.main()
