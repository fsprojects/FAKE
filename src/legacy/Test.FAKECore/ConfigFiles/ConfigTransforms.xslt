<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:output method="xml" indent="yes"/>

    <xsl:template match="appSettings/add[@key='DatabaseName']">
      <add key="DatabaseName" value="XsltDatabaseNameChanged" />
    </xsl:template>

    <xsl:template match="connectionStrings/add[@name='basic']">
      <add name="basic" connectionString="XsltDatabaseConnectionStringChanged" />
    </xsl:template>
  
    <xsl:template match="@* | node()">
        <xsl:copy>
            <xsl:apply-templates select="@* | node()"/>
        </xsl:copy>
    </xsl:template>
</xsl:stylesheet>
