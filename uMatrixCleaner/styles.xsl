<?xml version="1.0" encoding="utf-8"?>
<!-- Created with Liquid Studio 2018 (https://www.liquid-technologies.com) -->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:template match="UMatrixRule|MasterRule">
        <xsl:value-of select="concat(Source/text(),' ', Destination/text(),' ')" />
        <xsl:value-of select="translate(Type/text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')" />
        <xsl:choose>
            <xsl:when test="IsAllow/text() = 'true' ">
                allow
            </xsl:when>
            <xsl:otherwise>
                block
            </xsl:otherwise>
        </xsl:choose>  
    </xsl:template>


    <xsl:template match="/">
        <html xsl:version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
            <style type="text/css">
                .ruleList {
                    display:inline-block;
                    vertical-align: top;
                    margin-top: 0;
                    margin: 0;
                    background-image: linear-gradient(darkblue, darkblue), linear-gradient(darkblue, darkblue), linear-gradient(darkblue, darkblue), linear-gradient(darkblue, darkblue);
                    background-repeat: no-repeat;
                    background-size: 8px 3px;
                    background-position: top left, top right, bottom left, bottom right;
                    border: solid darkblue;
                    border-width: 0 3px;
                    padding: 0.2em 1em;
                    list-style-type: none;
                }
                
                .sticky {
                    position: sticky;
                    top: 2em;
                }
            </style>
            <body>
                <h1>日志</h1>
                <xsl:for-each select="Events/*">
                    <fieldset>
                        <xsl:if test="name() = 'DedupRuleEvent'">
                            <legend>删除重复规则</legend>
                            <span class="sticky">删除 </span>
                            <ul class="ruleList">
                                <xsl:for-each select="DuplicateRules/UMatrixRule">
                                    <li>
                                        <xsl:apply-templates select="."/>
                                    </li>
                                </xsl:for-each>
                            </ul>
                            <span class="sticky">，因为与
                            <xsl:apply-templates select="MasterRule"/>
                            重复。</span>
                        </xsl:if>

                        <xsl:if test="name() = 'MergeEvent'">
                            <legend>合并相似规则</legend>
                            <span class="sticky">合并 </span>
                            <ul class="ruleList">
                                <xsl:for-each select="RulesToDelete/UMatrixRule">
                                    <li>
                                        <xsl:apply-templates select="."/>
                                    </li>
                                </xsl:for-each>
                            </ul>
                            <span class="sticky"> 为
                            <xsl:apply-templates select="MasterRule"/></span>
                        </xsl:if>
                    </fieldset>
                </xsl:for-each>
            </body>
        </html>
    </xsl:template>
</xsl:stylesheet>