import re
import openpyxl

# 读取txt文件
with open("CustomComboPreset.txt", "r") as f:
    txt_content = f.read()

# 使用正则表达式提取被""包裹起来的语句
pattern = re.compile(r'"([^"]*)"')
extracted_sentences = pattern.findall(txt_content)

# 将提取出的语句存储到xlsx文件的第一列
wb = openpyxl.Workbook()
sheet = wb.active

for i in range(len(extracted_sentences)):
    sheet.cell(row=i+1, column=1, value=extracted_sentences[i])

wb.save("output.xlsx")
