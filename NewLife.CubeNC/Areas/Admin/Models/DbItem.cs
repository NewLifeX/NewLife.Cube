﻿using XCode.DataAccessLayer;

namespace NewLife.Cube.Areas.Admin.Models;

/// <summary>数据项</summary>
public class DbItem : ICubeModel
{
    /// <summary>连接名</summary>
    public String Name { get; set; }

    /// <summary>数据库类型</summary>
    public DatabaseType Type { get; set; }

    /// <summary>连接字符串</summary>
    public String ConnStr { get; set; }

    /// <summary>数据库版本</summary>
    public String Version { get; set; }

    /// <summary>数据驱动</summary>
    public String Driver { get; set; }

    /// <summary>驱动版本</summary>
    public String DriverVersion { get; set; }

    /// <summary>实体数</summary>
    public Int32 Entities { get; set; }

    /// <summary>数据表数</summary>
    public Int32 Tables { get; set; }

    /// <summary>是否动态</summary>
    public Boolean Dynamic { get; set; }

    /// <summary>备份数</summary>
    public Int32 Backups { get; set; }
}