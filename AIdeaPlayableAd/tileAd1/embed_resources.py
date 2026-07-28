"""将 resource 下的 PNG 以 base64 data URI 形式写入 game2_unity.html 的 BASE64_IMAGES。

用法：python embed_resources.py
脚本是幂等的：已存在的 key 会被覆盖为最新内容，缺失的 key 会被插入。
"""
import base64
import os
import re

HERE = os.path.dirname(os.path.abspath(__file__))
HTML = os.path.join(HERE, "game2_unity.html")
RES = os.path.join(HERE, "resource")

# key -> 文件名。只在这里登记需要内嵌/更新的资源。
ASSETS = {
    "start_bg": "start_bg.png",
    "chosen": "chosen.png",
}


def data_uri(path):
    with open(path, "rb") as f:
        b64 = base64.b64encode(f.read()).decode("ascii")
    return "data:image/png;base64," + b64


def main():
    with open(HTML, "r", encoding="utf-8") as f:
        text = f.read()

    for key, fname in ASSETS.items():
        uri = data_uri(os.path.join(RES, fname))
        entry = "            '%s': '%s', // %s\n" % (key, uri, fname)

        # 已存在则整行替换（带可选行尾注释）。
        pattern = re.compile(
            r"^[ \t]*'%s'[ \t]*:[ \t]*'[^']*',?.*\r?\n" % re.escape(key),
            re.MULTILINE,
        )
        if pattern.search(text):
            text = pattern.sub(entry, text, count=1)
            print("updated  %s -> %s" % (key, fname))
            continue

        # 不存在则插入到 'arrow' 条目之前（arrow 是对象最后一项）。
        anchor = re.compile(r"^([ \t]*'arrow'[ \t]*:)", re.MULTILINE)
        m = anchor.search(text)
        if not m:
            raise SystemExit("找不到 'arrow' 锚点，无法插入 %s" % key)
        text = text[: m.start()] + entry + text[m.start():]
        print("inserted %s -> %s" % (key, fname))

    with open(HTML, "w", encoding="utf-8") as f:
        f.write(text)
    print("done. html size =", os.path.getsize(HTML))


if __name__ == "__main__":
    main()
