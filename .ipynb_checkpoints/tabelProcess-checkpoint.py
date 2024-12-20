import pandas as pd
import numpy as np

def read_tabel(file_path):
    return pd.read_csv(file_path,encoding='utf-8')

df=read_tabel('E:\GIS开发实习材料\实习1\hebei.gdb\TemData.csv')#读取生成的表
coordinates=df[['POINT_X','POINT_Y','GDP']].values#获取关键数值
attractive_matrix=np.zeros((len(coordinates),len(coordinates)))#初始化吸引力零矩阵
for i in range(len(coordinates)):
    for j in range(len(coordinates)):
        if i!=j:
            attractive_matrix[i,j]=coordinates[i,2]*coordinates[j,2]/(coordinates[i,0]-coordinates[j,0])**2+(coordinates[i,1]-coordinates[j,1])**2#计算吸引力
attractive_df=pd.DataFrame(attractive_matrix)
attractive_max_index=[]#创建索引列表
attractive_max_num=[]#创建最大吸引力列表
print(attractive_matrix)
for i in range(len(coordinates)):
    attractive_max_index.append(np.argmax(attractive_matrix[i,:]))
    attractive_max_num.append(np.max(attractive_matrix[i,:]))

newTable=pd.DataFrame(index=range(len(coordinates)),columns=["POINT","START_POINT","END_POINT","ATTRACTION","START_X","START_Y","END_X","END_Y"])
for i in range(len(coordinates)):
    newTable.iloc[i]["POINT"]=df.loc[i,"Name"]
    if(df.loc[i,"GDP"]<df.loc[attractive_max_index[i],"GDP"]):
        newTable.loc[i,"START_POINT"]=df.loc[i,"Name"]
        newTable.loc[i,"END_POINT"]=df.loc[attractive_max_index[i],"Name"]
        newTable.loc[i,"ATTRACTION"]=attractive_max_num[i]
        newTable.loc[i,"START_X"]=coordinates[i,0]
        newTable.loc[i,"START_Y"]=coordinates[i,1]
        newTable.loc[i,"END_X"]=coordinates[attractive_max_index[i],0]
        newTable.loc[i,"END_Y"]=coordinates[attractive_max_index[i],1]
    else:
        newTable.loc[i,"START_POINT"]=df.loc[attractive_max_index[i],"Name"]
        newTable.loc[i,"END_POINT"]=df.loc[i,"Name"]
        newTable.loc[i,"ATTRACTION"]=attractive_max_num[i]
        newTable.loc[i,"START_X"]=coordinates[attractive_max_index[i],0]
        newTable.loc[i,"START_Y"]=coordinates[attractive_max_index[i],1]
        newTable.loc[i,"END_X"]=coordinates[i,0]
        newTable.loc[i,"END_Y"]=coordinates[i,1]
newTable.to_csv(r"E:\GIS开发实习材料\实习1\testtable_processed1.csv",encoding='utf-8',index=False)
