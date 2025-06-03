<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="2.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:fo="http://www.w3.org/1999/XSL/Format" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns="http://my-company.com/namespace"
 
>
  <xsl:output method="html" encoding="UTF-8"/>

  <xsl:template match="/">

    <html>
      <head>
        <title>Съобщение получено от СВ</title>


        <meta http-equiv="Content-Type" content="text/html;  charset=utf-8"/>



        <style type="text/css">
          body {
          font-family: Verdana, Arial, Helvetica, sans-serif;
          font-size:10px;
          }
          .value {
          font-weight: bold;
          color: #440000;
          }
          .tablePart td {
          border-bottom: 1px solid #333333;
          border-left: 1px solid #333333;
          }
          table.tablePart {
          empty-cells:show;
          border-top: 1px solid #333333;
          border-right: 1px solid #333333;

          }
          table {
          font-size:10px;
          empty-cells:show;
          }
          h2 {
          font-size:12px;
          }
          h3 {
          font-size:11px;
          }
          h4 {
          font-size:10px;
          }
        </style>
      </head>

      <body>

        <table width="600" align="center" border="0">
          <xsl:for-each select="LOTS">
            <tr>
              <td>
                <xsl:apply-templates select="lot"/>
              </td>
            </tr>
          </xsl:for-each>
          <tr>
            <td>
              <xsl:apply-templates select="lot"/>
            </td>
          </tr>
        </table>
      </body>
    </html>
  </xsl:template>


  <xsl:template match="lot">
    <h2 align="center">
      <b>ИМОТЕН РЕГИСТЪР</b>
    </h2>
    <br></br>
    <br>
      <h3 align="center">
        СЛУЖБА ПО ВПИСВАНИЯТА-  гр. <xsl:value-of select="RegistryOffice"/>
      </h3>
    </br>
    <h3 align="center">
      Номер на партидата:
      <span class="value">
        <xsl:value-of select="LotNo"/>
      </span>
    </h3>

    <table class="tablePart" cellspacing="0" width="100%">
      <tr>
        <td width="50%">
          Открита на:
          <span class="value">
            <xsl:value-of select="substring(LotOpenDate,1,10)"/>
          </span>
        </td>
        <td width="50%">
          Закрита на:
          <span class="value">
            <xsl:value-of select="substring(LotCloseDate,1,10)"/>
          </span>
        </td>
      </tr>

      <tr>
        <td width="50%">
          Определение №
          <span class="value">
            <xsl:value-of select="LotOpenSpecification"/>
          </span>
        </td>
        <td width="50%">
          Определение №
          <span class="value">
            <xsl:value-of select="LotCloseSpecification"/>
          </span>
        </td>
      </tr>
      <tr>
        <td width="50%">
          Предходна/и партида/и:
          <xsl:for-each select="PRECEDELOTS">
            №
            <span class="value">
              <xsl:value-of select="PrecedeLot"/>
            </span>
          </xsl:for-each>
          <br/>Партиди на имоти към които са присъединени части от имота
          <xsl:for-each select="COHERENTLOTS">
            №
            <span class="value">
              <xsl:value-of select="CoherentLot"/>
            </span>
          </xsl:for-each>
          <br/>Партиди на отделно притежавани сгради и самостоятелни обекти в сгради
          <xsl:for-each select="SUBOBJECTLOTS">
            №
            <span class="value">
              <xsl:value-of select="SubLot"/>
            </span>
          </xsl:for-each>
        </td>

        <td width="50%" valign="top">
          Следваща/и партида/и:
          <xsl:for-each select="NEXTLOTS">
            №
            <span class="value">
              <xsl:value-of select="NextLot"/>
            </span>
          </xsl:for-each>
        </td>
      </tr>
    </table>
    <xsl:variable name="AEkatte">
      <xsl:value-of select="lotData/A/cadNum/@ekatte"/>
    </xsl:variable>
    <xsl:variable name="ACadRegion">
      <xsl:value-of select="lotData/A/cadNum/@cadregion"/>
    </xsl:variable>
    <xsl:variable name="ACadimmovable">
      <xsl:value-of select="lotData/A/cadNum/@cadimmovable"/>
    </xsl:variable>
    <xsl:variable name="Acadbuilding">
      <xsl:value-of select="lotData/A/cadNum/@cadbuilding"/>
    </xsl:variable>
    <xsl:variable name="ACadapp">
      <xsl:value-of select="lotData/A/cadNum/@cadapp"/>
    </xsl:variable>




    <xsl:for-each select="DATA/PROPERTIES/property">
      <xsl:variable name="insideCadNum">
        <xsl:value-of select="propertyDesc/cadDesc/cadNum"/>
      </xsl:variable>
      <xsl:if test="propertyDesc/cadDesc/cadNum[@ekatte = $AEkatte] and   propertyDesc/cadDesc/cadNum[@cadregion = $ACadRegion] and propertyDesc/cadDesc/cadNum[@cadimmovable = $ACadimmovable]">

        <h3 align="center">ЧАСТ "А"   ДАННИ ЗА ИМОТА</h3>

        <h4 align="center">
          ИДЕНТИФИКАТОР     <span class="value">
            <xsl:value-of select="$insideCadNum"/>
          </span>
        </h4>Идентификатори на кадастрални обекти към същия имот:


        <xsl:apply-templates select="SUBOBJECTS"/>

        <xsl:apply-templates select="propertyDesc"/>

        <xsl:for-each select="NEIGHBOURS">

          <br/>
          <b>Съседи:</b>
          <table width="100%" cellspacing="0" class="tablePart">
            <xsl:for-each select="neighbour">
              <tr>
                <td width="200px" valign="top">
                  Кад. №
                  <span class="value">
                    <xsl:if test="cadNum/@ekatte[.!='0']">
                      <xsl:value-of select="cadNum/@ekatte"/>
                    </xsl:if>
                    <xsl:if test="cadNum/@cadregion[.!='0']">
                      .<xsl:value-of select="cadNum/@cadregion"/>
                    </xsl:if>
                    <xsl:if test="cadNum/@cadimmovable[.!='0']">
                      .<xsl:value-of select="cadNum/@cadimmovable"/>
                    </xsl:if>
                    <xsl:if test="cadNum/@cadbuilding[.!='0']">
                      .<xsl:value-of select="cadNum/@cadbuilding"/>
                    </xsl:if>
                    <xsl:if test="cadNum/@cadapp[.!='0']">
                      .<xsl:value-of select="cadNum/@cadapp"/>
                    </xsl:if>
                  </span>
                </td>
                <td width="400px" valign="top">
                  Описание: <span class="value">
                    <xsl:value-of select="neighbourDesc"/>
                  </span>
                </td>
              </tr>
            </xsl:for-each>
          </table>
        </xsl:for-each>
      </xsl:if>
    </xsl:for-each>

    <br/>
    <h3 align="center">ЧАСТ "Б" ДАННИ ЗА СОБСТВЕНИКА И ЗА ПРИЗНАВАНЕТО И ПРЕХВЪРЛЯНЕТО НА ПРАВОТО НА СОБСТВЕНОСТ</h3>
    <xsl:apply-templates select="lotData/B"/>

    <br/>
    <h3 align="center">
      ЧАСТ "В" ДАННИ ЗА УЧРЕДЯВАНЕ И ПРЕХВЪРЛЯНЕ НА ДРУГИ ВЕЩНИ ПРАВА И ЗА ПОДЛЕЖАЩИТЕ НА
      ВПИСВАНЕ ЮРИДИЧЕСКИ ФАКТИ И ОБСТОЯТЕЛСТВА ОСВЕН ИПОТЕКИ И ВЪЗБРАНИ
    </h3>
    <xsl:apply-templates select="lotData/C"/>


    <br/>
    <h3 align="center">ЧАСТ "Г" ДАННИ ЗА ИПОТЕКИТЕ</h3>
    <xsl:apply-templates select="lotData/D"/>



    <br/>
    <h3 align="center">ЧАСТ "Д" ДАННИ ЗА ВЪЗБРАНИТЕ</h3>
    <xsl:apply-templates select="lotData/E"/>
  </xsl:template>






  <xsl:template match="lotData/B">
    <xsl:apply-templates select="right"/>

    <xsl:if test="actId[.!='']">

      <br/>
      <b>Искови молби:</b>
      <table width="100%" cellspacing="0" class="tablePart">
        <tr>
          <td width="200px">
            <b>Номер/дата</b>
          </td>
          <td width="200px">
            <b>Вид</b>
          </td>
          <td width="200px">
            <b>Предмет на спора</b>
          </td>
        </tr>
        <xsl:for-each select="actId">
          <xsl:variable name="claimID">
            <xsl:value-of select="."/>
          </xsl:variable>
          <xsl:apply-templates select="../../../DATA/ACTS/act[@id=$claimID]">
            <xsl:with-param name="type">1</xsl:with-param>
          </xsl:apply-templates>
        </xsl:for-each>
      </table>
    </xsl:if>
    <xsl:if test="application[.!='']">

      <br/>
      <b>Откази и жалби:</b>
      <table width="100%" cellspacing="0" class="tablePart">
        <tr>
          <td width="200px">
            <b>Входяща молба</b>
          </td>
          <td width="200px">
            <b>Отказ</b>
          </td>
          <td width="200px">
            <b>Жалба</b>
          </td>
        </tr>
        <xsl:apply-templates select="application"/>
      </table>
    </xsl:if>
  </xsl:template>




  <xsl:template match="lotData/C">

    <xsl:apply-templates select="right"/>

    <xsl:if test="actId[.!='']">

      <br/>
      <b>Искови молби:</b>
      <table width="100%" cellspacing="0" class="tablePart">
        <tr>
          <td width="200px">
            <b>Номер/дата</b>
          </td>
          <td width="200px">
            <b>Вид</b>
          </td>
          <td width="200px">
            <b>Предмет на спора</b>
          </td>
        </tr>
        <xsl:for-each select="actId">
          <xsl:variable name="claimID">
            <xsl:value-of select="."/>
          </xsl:variable>
          <xsl:apply-templates select="../../../DATA/ACTS/act[@id=$claimID]">
            <xsl:with-param name="type">1</xsl:with-param>
          </xsl:apply-templates>
        </xsl:for-each>
      </table>
    </xsl:if>

    <xsl:if test="application[.!='']">

      <br/>
      <b>Откази и жалби:</b>
      <table width="100%" cellspacing="0" class="tablePart">
        <tr>
          <td width="200px">
            <b>Входяща молба</b>
          </td>
          <td width="200px">
            <b>Отказ</b>
          </td>
          <td width="200px">
            <b>Жалба</b>
          </td>
        </tr>
        <xsl:apply-templates select="application"/>
      </table>
    </xsl:if>
  </xsl:template>




  <xsl:template match="lotData/D">

    <xsl:if test="actId[.!='']">

      <br/>
      <b>Описание:</b>
      <table width="100%" cellspacing="0" class="tablePart">
        <xsl:for-each select="actId">
          <xsl:variable name="claimID">
            <xsl:value-of select="."/>
          </xsl:variable>
          <xsl:apply-templates select="../../../DATA/ACTS/act[@id=$claimID]">
            <xsl:with-param name="type">2</xsl:with-param>
          </xsl:apply-templates>
        </xsl:for-each>
      </table>
    </xsl:if>

    <xsl:if test="application[.!='']">

      <br/>
      <b>Откази и жалби:</b>
      <table width="100%" cellspacing="0" class="tablePart">
        <tr>
          <td width="200px">
            <b>Входяща молба</b>
          </td>
          <td width="200px">
            <b>Отказ</b>
          </td>
          <td width="200px">
            <b>Жалба</b>
          </td>
        </tr>
        <xsl:apply-templates select="application"/>
      </table>
    </xsl:if>
  </xsl:template>



  <xsl:template match="lotData/E">
    <xsl:if test="actId[.!='']">

      <br/>
      <b>Описание:</b>
      <table width="100%" cellspacing="0" class="tablePart">
        <xsl:for-each select="actId">
          <xsl:variable name="claimID">
            <xsl:value-of select="."/>
          </xsl:variable>
          <xsl:apply-templates select="../../../DATA/ACTS/act[@id=$claimID]">
            <xsl:with-param name="type">2</xsl:with-param>
          </xsl:apply-templates>
        </xsl:for-each>
      </table>
    </xsl:if>

    <xsl:if test="application[.!='']">
      <tr>
        <td>
          <br/>
          <b>Откази и жалби:</b>
          <table width="100%" cellspacing="0" class="tablePart">
            <tr>
              <td width="200px">
                <b>Входяща молба</b>
              </td>
              <td width="200px">
                <b>Отказ</b>
              </td>
              <td width="200px">
                <b>Жалба</b>
              </td>
            </tr>
            <xsl:apply-templates select="application"/>
          </table>
        </td>
      </tr>
    </xsl:if>
  </xsl:template>


  <xsl:template match="right">

    <xsl:variable name="entityID">
      <xsl:value-of select="entityID"/>
    </xsl:variable>
    <xsl:variable name="actID">
      <xsl:value-of select="actID"/>
    </xsl:variable>
    <b>Описание:</b>
    <table width="100%" cellspacing="0" class="tablePart">

      <xsl:apply-templates select="../../../DATA/ACTS/act[@id=$actID]">
        <xsl:with-param name="type">2</xsl:with-param>
      </xsl:apply-templates>

      <xsl:apply-templates select="../../../DATA/ENTITIES/entity[@id=$entityID]"/>

      <tr>
        <td width="200px">Данни за правото</td>
        <td width="400px">
          <xsl:if test="rightType[.!='']">
            вид на правото: <span class="value">
              <xsl:value-of select="rightType"/>,
            </span>
          </xsl:if>
          <xsl:if test="nominator[.!='']">
            идеална част ном/деном: <span class="value">
              <xsl:value-of select="nominator"/>/<xsl:value-of select="denominator"/> ,
            </span>
          </xsl:if>
          <xsl:if test="percent[.!='']">
            идеална част процент: <span class="value">
              <xsl:value-of select="percent"/>,
            </span>
          </xsl:if>
          <xsl:if test="docArea[.!='']">
            идеална част площ: <span class="value">
              <xsl:value-of select="docArea"/>,
            </span>
          </xsl:if>
          <xsl:if test="startDate[.!='']">
            начална дата: <span class="value">
              <xsl:value-of select="substring(startDate,1,10)"/>,
            </span>
          </xsl:if>
          <xsl:if test="endDate[.!='']">
            дата на прекратяване на правото: <span class="value">
              <xsl:value-of select="substring(endDate,1,10)"/>,
            </span>
          </xsl:if>
          <xsl:if test="changeFlag[.!='']">
            флаг за вида на правото: <span class="value">
              <xsl:value-of select="changeFlag"/>,
            </span>
          </xsl:if>
          <xsl:if test="remark[.!='']">
            <br/> забележка: <span class="value">
              <xsl:value-of select="remark"/>
            </span>
          </xsl:if>
        </td>
      </tr>
    </table>
  </xsl:template>

  <xsl:template match="application">
    <tr>
      <td width="200px">
        &#xA0;
        <xsl:value-of select="AppNo"/> / <xsl:value-of select="substring(RegistrationDate,1,10)"/>
      </td>
      <td width="200px">
        &#xA0;
        <xsl:value-of select="substring(rejection/RejectionDate,1,10)"/> / <xsl:value-of select="rejection/RejectionStatus"/>
      </td>

      <td width="200px">
        &#xA0;
        <xsl:for-each select="rejection/appeal">
          <xsl:value-of select="substring(AppealDate,1,10)"/> / Статус: <xsl:value-of select="AppealStatus"/> / Дата на статус: <xsl:value-of select="substring(AppealStatusDate,1,10)"/> / <xsl:value-of select="AppealRemark"/>
        </xsl:for-each>
      </td>
    </tr>
  </xsl:template>



  <xsl:template match="entity">
    <tr>
      <td width="200px">Лица </td>
      <td width="400px">
        Вид на субекта: <span class="value">
          <xsl:value-of select="type"/>,
        </span>
        Собственик:<span class="value">
          <xsl:if test="first[.!='']">
            &#xA0;<xsl:value-of select="first"/>
          </xsl:if>
          <xsl:if test="middle[.!='']">
            &#xA0;<xsl:value-of select="middle"/>
          </xsl:if>
          <xsl:if test="family[.!='']">
            &#xA0;<xsl:value-of select="family"/>
          </xsl:if>
          <xsl:if test="alias[.!='']">
            &#xA0;<xsl:value-of select="alias"/>
          </xsl:if>,
        </span>
        <xsl:if test="egnBulstat[.!='']">
          ЕГН/БУЛСТАТ: <span class="value">
            <xsl:value-of select="egnBulstat"/>,
          </span>
        </xsl:if>
        <xsl:for-each select="ADDRESSES">
          Адреси: <br/>
          <xsl:apply-templates select="address"/>
        </xsl:for-each>
      </td>
    </tr>
  </xsl:template>



  <xsl:template match="act">
    <xsl:param name="type"/>
    <xsl:if test="$type = 1">
      <tr>
        <td width="200px">
          &#xA0;
          <xsl:value-of select="registration/doubleRegisterNumber"/> / <xsl:value-of select="substring(registration/date,1,10)"/>, Книга: <xsl:value-of select="registration/book"/>, том <xsl:value-of select="registration/volume"/>, Страница <xsl:value-of select="registration/page"/>, Тип на акта: <xsl:value-of select="type"/>
        </td>
        <td width="200px">
          &#xA0;
          <xsl:value-of select="issuer/court/caseType"/>
        </td>
        <td width="200px">
          &#xA0;
          <xsl:value-of select="remark"/>
        </td>
      </tr>
    </xsl:if>
    <xsl:if test="$type = 2">
      <tr>
        <td width="200px">Данни за акта:</td>
        <td width="400px">
          <span class="value">
            <xsl:value-of select="type"/>
          </span>
          <xsl:if test="issuer/court/court[.!='']">
            Издател:
            <span class="value">
              <xsl:value-of select="issuer/court/court"/>,
            </span>
          </xsl:if>
          <xsl:if test="issuer/court/caseNumber[.!='']">
            № на делото: <span class="value">
              <xsl:value-of select="issuer/court/caseNumber"/>,
            </span>
          </xsl:if>
          <xsl:if test="registration/book[.!='']">
            книга: <span class="value">
              <xsl:value-of select="registration/book"/>,
            </span>
          </xsl:if>
          <xsl:if test="registration/volume[.!='']">
            том:  <span class="value">
              <xsl:value-of select="registration/volume"/>,
            </span>
          </xsl:if>
          <xsl:if test="registration/page[.!='']">
            № <span class="value">
              <xsl:value-of select="registration/page"/>,
            </span>
          </xsl:if>
          <xsl:if test="registration/date[.!='']">
            дата: <span class="value">
              <xsl:value-of select="substring(registration/date,1,10)"/>,
            </span>
          </xsl:if>
          <xsl:if test="registration/doubleRegisterNumber[.!='']">
            двойно вх. № <span class="value">
              <xsl:value-of select="registration/doubleRegisterNumber"/>,
            </span>
          </xsl:if>
          <xsl:if test="remark[.!='']">
            <br/> забележка:
            <span class="value">
              <xsl:value-of select="remark"/>
            </span>
          </xsl:if>
        </td>
      </tr>
    </xsl:if>
  </xsl:template>



  <xsl:template match="SUBOBJECTS">
    <br/>Сгради:
    <xsl:for-each select="SubObject[@objtype='2']">
      <xsl:value-of select="."/> ;
    </xsl:for-each>
    <br></br>Самостоятелни обекти в сграда:
    <xsl:for-each select="SubObject[@objtype='3']">
      <xsl:value-of select="."/>
    </xsl:for-each>
  </xsl:template>

  <xsl:template match="address">
    <xsl:if test="province[.!='']">
      Област: <span class="value">
        <xsl:value-of select="province"/>,
      </span>
    </xsl:if>
    <xsl:if test="municipality[.!='']">
      Община: <span class="value">
        <xsl:value-of select="municipality"/>,
      </span>
    </xsl:if>
    <xsl:if test="ekatte[.!='']">
      Населено място: <span class="value">
        <xsl:value-of select="ekatte"/>,
      </span>
    </xsl:if>
    <xsl:if test="EKATTE/@ekatteCode[.!='']">
      ЕКАТТЕ: <span class="value">
        <xsl:value-of select="EKATTE/@ekatteCode"/>,
      </span>
    </xsl:if>
    <xsl:if test="region[.!='']">
      Район: <span class="value">
        <xsl:value-of select="region"/>,
      </span>
    </xsl:if>
    <xsl:if test="postCode[.!='']">
      П.к.: <span class="value">
        <xsl:value-of select="postCode"/>,
      </span>
    </xsl:if>
    <xsl:if test="quarter[.!='']">
      Кв.: <span class="value">
        <xsl:value-of select="quarter"/>,
      </span>
    </xsl:if>
    <xsl:if test="street[.!='']">
      Ул.: <span class="value">
        <xsl:value-of select="street"/>,
      </span>
    </xsl:if>
    <xsl:if test="streetNum[.!='']">
      № <span class="value">
        <xsl:value-of select="streetNum"/>,
      </span>
    </xsl:if>
    <xsl:if test="streetSubNum[.!='']">
      Под  № <span class="value">
        <xsl:value-of select="streetSubNum"/>,
      </span>
    </xsl:if>
    <xsl:if test="blockNum[.!='']">
      бл. <span class="value">
        <xsl:value-of select="blockNum"/>,
      </span>
    </xsl:if>
    <xsl:if test="entrance[.!='']">
      вх. <span class="value">
        <xsl:value-of select="entrance"/>,
      </span>
    </xsl:if>
    <xsl:if test="floorNum[.!='']">
      ет. <span class="value">
        <xsl:value-of select="floorNum"/>,
      </span>
    </xsl:if>
    <xsl:if test="appNum[.!='']">
      ап. <span class="value">
        <xsl:value-of select="appNum"/>
      </span>
    </xsl:if>
  </xsl:template>




  <xsl:template match="propertyDesc">
    <br/>
    <br/>
    <b>Описание:</b>
    <table width="100%" cellspacing="0" class="tablePart">
      <xsl:for-each select="cadDesc">
        <tr>
          <td width="200">Вид на имота:</td>
          <td width="400px">
            <span class="value">
              &#xA0;
              <xsl:value-of select="propertyType"/>
            </span>
          </td>
        </tr>
        <tr>
          <td>Предназначение:</td>
          <td>
              &#xA0;
	 			<xsl:if test="cadNum[@immType = '3']">
                  <span class="value">
                    <xsl:value-of select="funcType"/>
                  </span>
                </xsl:if>
                <xsl:if test="cadNum[@immType = '2']">
                  <span class="value">
                    <xsl:value-of select="funcType"/>
                  </span>
                </xsl:if>
                <xsl:if test="cadNum[@immType = '1']">
                  <span class="value">
                    <xsl:value-of select="purposeType"/>
                  </span>
                </xsl:if>
          </td>
        </tr>
        <tr>
          <td>Площ:</td>
          <td>
            <span class="value">
              &#xA0;

              <xsl:value-of select="propertyArea"/>
            </span>
          </td>
        </tr>
      </xsl:for-each>
      <tr>
        <td>Адрес:</td>
        <td>
          &#xA0;


          <xsl:for-each select="ADDRESSES">
            <xsl:apply-templates select="address"/>
          </xsl:for-each>
        </td>
      </tr>
      <tr>
        <td>Старо кадастрално описание:</td>
        <td>
          &#xA0;


          <xsl:for-each select="oldCad">
            <xsl:if test="plsnNum[.!='']">
              План. № <span class="value">
                <xsl:value-of select="plsnNum"/>,
              </span>
            </xsl:if>
            <xsl:if test="plsnKv[.!='']">
              лан. кв:<span class="value">
                <xsl:value-of select="plsnKv"/>,
              </span>
            </xsl:if>
            <xsl:if test="plsnRegName[.!='']">
              План. рег.: <span class="value">
                <xsl:value-of select="plsnRegName"/>,
              </span>
            </xsl:if>
            <xsl:if test="cadList[.!='']">
              Кад. лист: <span class="value">
                <xsl:value-of select="cadList"/>
              </span>
            </xsl:if>
          </xsl:for-each>
        </td>
      </tr>
      <tr>
        <td>Регулация:</td>
        <td>
          &#xA0;


          <xsl:for-each select="regulation">
            <xsl:if test="regParcel[.!='']">
              Парцел: <span class="value">
                <xsl:value-of select="regParcel"/>,
              </span>
            </xsl:if>
            <xsl:if test="regQuarter[.!='']">
              Квартал: <span class="value">
                <xsl:value-of select="regQuarter"/>,
              </span>
            </xsl:if>
            <xsl:if test="regPlan[.!='']">
              План: <span class="value">
                <xsl:value-of select="regPlan"/>
              </span>
            </xsl:if>
          </xsl:for-each>
        </td>
      </tr>
      <tr>
        <td>Описание на КВС</td>
        <td>
          &#xA0;

          <xsl:for-each select="restoredPropertyMap">
            <xsl:if test="placeName[.!='']">
              Населено място: <span class="value">
                <xsl:value-of select="placeName"/>,
              </span>
            </xsl:if>
            <xsl:if test="massiv[.!='']">
              Масив: <span class="value">
                <xsl:value-of select="massiv"/>,
              </span>
            </xsl:if>
            <xsl:if test="parcel[.!='']">
              Парцел: <span class="value">
                <xsl:value-of select="parcel"/>
              </span>
            </xsl:if>
          </xsl:for-each>
        </td>
      </tr>
    </table>
    <!--  propertyDesc  -->
  </xsl:template>
</xsl:stylesheet>
<!-- Stylus Studio meta-information - (c) 2004-2009. Progress Software Corporation. All rights reserved.

<metaInformation>
	<scenarios>
		<scenario default="yes" name="Scenario1" userelativepaths="yes" externalpreview="no" url="..\..\..\..\..\..\Users\gmanchev\Desktop\RA_2412 - Copy.xml" htmlbaseurl="" outputurl="" processortype="saxon8" useresolver="yes" profilemode="0"
		          profiledepth="" profilelength="" urlprofilexml="" commandline="" additionalpath="" additionalclasspath="" postprocessortype="none" postprocesscommandline="" postprocessadditionalpath="" postprocessgeneratedext="" validateoutput="no"
		          validator="internal" customvalidator="">
			<advancedProp name="sInitialMode" value=""/>
			<advancedProp name="bXsltOneIsOkay" value="true"/>
			<advancedProp name="bSchemaAware" value="true"/>
			<advancedProp name="bGenerateByteCode" value="true"/>
			<advancedProp name="bXml11" value="false"/>
			<advancedProp name="iValidation" value="0"/>
			<advancedProp name="bExtensions" value="true"/>
			<advancedProp name="iWhitespace" value="0"/>
			<advancedProp name="sInitialTemplate" value=""/>
			<advancedProp name="bTinyTree" value="true"/>
			<advancedProp name="xsltVersion" value="2.0"/>
			<advancedProp name="bWarnings" value="true"/>
			<advancedProp name="bUseDTD" value="false"/>
			<advancedProp name="iErrorHandling" value="fatal"/>
		</scenario>
	</scenarios>
	<MapperMetaTag>
		<MapperInfo srcSchemaPathIsRelative="yes" srcSchemaInterpretAsXML="no" destSchemaPath="" destSchemaRoot="" destSchemaPathIsRelative="yes" destSchemaInterpretAsXML="no"/>
		<MapperBlockPosition></MapperBlockPosition>
		<TemplateContext></TemplateContext>
		<MapperFilter side="source"></MapperFilter>
	</MapperMetaTag>
</metaInformation>
-->