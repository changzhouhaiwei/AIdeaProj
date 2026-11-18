from openpyxl import Workbook
from pathlib import Path

root = Path(__file__).resolve().parent


def save(name, rows, sheet):
    wb = Workbook()
    ws = wb.active
    ws.title = sheet
    for row in rows:
        ws.append(row)
    path = root / name
    if sheet == "Sheet1" and name == "__beans__.xlsx":
        ws.merge_cells("H1:L1")
    if sheet == "Sheet1" and name == "__enums__.xlsx":
        ws.merge_cells("G1:K1")
    wb.save(path)
    print("wrote", path.name)


save(
    "__tables__.xlsx",
    [
        ["##var", "full_name", "value_type", "define_from_file", "input", "index", "mode", "group", "comment", "patch_input", "tags", "output", "options"],
        ["##", "全名(包含模块和名字)", "记录类名", "从excel读取定义", "文件列表", "表id字段", "模式", "分组", "注释", "分支文件列表", None, "输出文件名", None],
        ["##", None, None, "false时取已有定义，true为从excel标题头和属性栏读取定义", "可以多个，以逗号','分隔", "为空则取第一个字段", "one|map|list，空为map", "c|s|e，空表示全部分组", None, None, None, None, None],
        [None, "Activity.TbWinStreak", "WinStreakCfg", True, "CSVWinStreak@WinStreak.xlsx", None, None, "c", "连胜活动", None, None, None, None],
        [None, "Activity.TbSevenDaySign", "SevenDaySignCfg", True, "CSVSevenDaySign@SevenDaySign.xlsx", None, None, "c", "七日签到", None, None, None, None],
        [None, "Setting.TbSetting", "SettingCfg", True, "CSVSetting@Settings.xlsx", None, None, "c", "设置", None, None, None, None],
        [None, "User.TbUserInfo", "UserInfoCfg", True, "CSVUserInfo@UserInfo.xlsx", None, None, "c", "用户信息", None, None, None, None],
    ],
    "Sheet1",
)

save(
    "__enums__.xlsx",
    [
        ["##var", "full_name", "flags", "unique", "comment", "tags", "*items", None, None, None, None],
        ["##var", None, None, None, None, None, "name", "alias", "value", "comment", "tags"],
        ["##", "全名(包含模块和名字)", "是否为位标记枚举", "枚举项是否唯一", None, None, "枚举名", "别名", "值", "注释", None],
    ],
    "Sheet1",
)

save(
    "__beans__.xlsx",
    [
        ["##var", "full_name", "parent", "sep", "alias", "comment", "tags", "*fields", None, None, None, None, None],
        ["##var", None, None, None, None, None, None, "name", "type", "group", "comment", "tags", None],
        ["##", "全名(包含模块和名字)", None, "分割符", None, None, None, "字段名", "类型", "分组", "注释", None, None],
        [None, None, None, None, None, None, None, None, None, None, None, None, None],
        [None, "Common.VectorInt", None, ",", None, None, None, "x", "int", None, None, None, None],
        [None, None, None, None, None, None, None, "y", "int", None, None, None, None],
    ],
    "Sheet1",
)


def data_book(filename, sheet, headers, types, comments, sample):
    rows = [
        ["##var", *headers],
        ["##type", *types],
        ["##", *comments],
        ["##", *headers],
        ["##", *types],
        [None, *sample],
    ]
    save(filename, rows, sheet)


data_book(
    "WinStreak.xlsx",
    "CSVWinStreak",
    ["id", "streak", "award"],
    ["int", "int", "string"],
    ["唯一Id", "连胜天数", "奖励，格式 type,count"],
    [1, 1, "1,10"],
)
data_book(
    "SevenDaySign.xlsx",
    "CSVSevenDaySign",
    ["id", "day", "award"],
    ["int", "int", "string"],
    ["唯一Id", "第几天", "奖励，格式 type,count"],
    [1, 1, "1,10"],
)
data_book(
    "Settings.xlsx",
    "CSVSetting",
    ["id", "key", "value"],
    ["int", "string", "string"],
    ["唯一Id", "设置项", "默认值"],
    [1, "music", "1"],
)
data_book(
    "UserInfo.xlsx",
    "CSVUserInfo",
    ["id", "nicknameMax", "defaultAvatar"],
    ["int", "int", "int"],
    ["唯一Id", "昵称最大长度", "默认头像"],
    [1, 16, 1],
)

langs = [
    "English",
    "Spanish (Mexico)",
    "Portuguese (Brazil)",
    "Russian",
    "German",
    "French",
    "Turkish",
    "Simplified Chinese",
    "Indonesian",
    "Traditional Chinese",
    "Italian",
    "Japanese",
    "Korean",
    "Ukrainian",
]
entries = [
    ("Settings", "Settings", "设置"),
    ("UserInfo", "Profile", "用户信息"),
    ("WinStreak", "Win Streak", "连胜"),
    ("SevenDaySign", "7-Day Sign-in", "七日签到"),
]
zh_index = langs.index("Simplified Chinese")
loc_rows = [["Key", "Type", "Desc", *langs]]
for key, en, zh in entries:
    values = [None] * len(langs)
    values[0] = en
    values[zh_index] = zh
    loc_rows.append([key, "Text", None, *values])
save("LocalizationConfig.xlsx", loc_rows, "LocalizationConfig")
