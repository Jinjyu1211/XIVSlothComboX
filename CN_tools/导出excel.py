import openpyxl

# 读取 xlsx 文件
workbook = openpyxl.load_workbook('替换表20230428.xlsx')
worksheet = workbook.active

# 构建替换字典
replace_dict = {}
for row in worksheet.iter_rows(values_only=True):
    replace_dict[row[0]] = row[1]

# 读取 txt 文件
with open('output1.txt', 'r',encoding='utf-8') as f:
    content = f.read()

# 替换文本
for key, value in replace_dict.items():
    print(f"Replacing '{key}' with '{value}'")
    if key is not None and value is not None:
        content = content.replace('"' + key + '"', '"' + value + '"')
        content = content.replace(' ' + key + ' ', ' ' + value + ' ')
        content = content.replace('\n' + key + '\n', '\n' + value + '\n')
    else:
        print(f"Skipping {key} -> {value} because it contains None")
        continue

# 输出替换后的 txt 文件
with open('output.txt', 'w',encoding='utf-8') as f:
    f.write(content)
