import path from "path";
import { fileURLToPath } from 'url';
const __dirname = path.dirname(fileURLToPath(import.meta.url));

const mode = process.env.NODE_ENV === 'development' ? 'development' : 'production';
const devtool = mode === 'production' ? 'source-map' : 'eval-source-map';
export default [
    {
        mode: mode,
        devtool: devtool,
        entry: {
            "core": './Scripts/Utilities/core.ts',
            "notification": './Scripts/Utilities/notification.ts',
            "resources": './Scripts/Utilities/resources.ts',
            "searchTable": './Scripts/Utilities/searchTable.ts',
            "signalr": './Scripts/Utilities/signalr.ts',
            "admin_logs": './Areas/Admin/Scripts/Logs.ts',
            "admin_translations": './Areas/Admin/Scripts/Translations.ts',
            "admin_nomenclatures": './Areas/Admin/Scripts/Nomenclatures.ts',
            "admin_clients": './Areas/Admin/Scripts/Clients.ts',
            "cms": './Areas/Admin/Scripts/Cms.ts',
            "admin_clientroles": './Areas/Admin/Scripts/ClientRoles.ts',
            "admin_clientattachments": './Areas/Admin/Scripts/ClientAttachments.ts',
            "admin_publications": './Areas/Admin/Scripts/Publications.ts',
            "admin_employeeroles": './Areas/Admin/Scripts/Employees/EmployeeRoles.ts',
            "admin_employees": './Areas/Admin/Scripts/Employees/Employees.ts',
            "admin_rolechangeorders": './Areas/Admin/Scripts/Employees/RoleChangeOrders.ts',
            "admin_applicationtypes": './Areas/Admin/Scripts/ApplicationTypes.ts',
            "outapplication": './Scripts/Controllers/OutApplication/OutApplication.ts',
            "outdocuments": './Scripts/Controllers/Documents/OutDocuments.ts',
            "admin_faq": './Areas/Admin/Scripts/Faq.ts',
            "account": "./Scripts/Controllers/Account/Account.ts",
            "sign": "./Scripts/Controllers/Sign/Sign.ts",
            "admin_outapplicationtypes": './Areas/Admin/Scripts/OutApplicationTypes.ts'
        },
        module: {
            rules: [
                {
                    test: /\.tsx?$/,
                    use: 'ts-loader',
                    exclude: /node_modules/
                }
            ],
        },
        resolve: {
            extensions: ['.tsx', '.ts', '.js'],
            modules: ['./node_modules/'],
        },
        output: {
            library: {
                name: "[name]",
                type: 'var',
            },
            filename: '[name].min.js',
            path: path.resolve(__dirname, 'wwwroot/bundles')
        },
        externals: [
            {
                jquery: 'jQuery',
                '@progress/kendo-ui': 'kendo',
                'scripts/Utilities/core': "core",
                'scripts/Utilities/notification': "notification",
                'scripts/Utilities/resources': "resources",
                'scripts/Utilities/searchTable': "searchTable",
                'scripts/Utilities/signalr': "signalr",
                'scripts/Application/application': "application",
                'scripts/Map/cart': "cart",
                'scripts/Map/index': "map",
            }
        ]
    },
    {
        mode: mode,
        devtool: devtool,
        entry: {
            "sessionTimeOut": './Scripts/Utilities/sessionTimeOut.ts'
        },
        module: {
            rules: [
                {
                    test: /\.tsx?$/,
                    use: 'ts-loader',
                    exclude: /node_modules/
                }
            ],
        },
        resolve: {
            extensions: ['.tsx', '.ts', '.js'],
            modules: ['./node_modules/'],
        },
        output: {
            library: "[name]",
            libraryTarget: "umd",
            libraryExport: "default",
            filename: '[name].min.js',
            path: path.resolve(__dirname, 'wwwroot/bundles')
        },
        externals: [
            {
                jquery: 'jQuery',
                '@progress/kendo-ui': 'kendo',
                'scripts/Utilities/core': "core",
                'scripts/Utilities/notification': "notification",
                'scripts/Utilities/resources': "resources",
                'scripts/Utilities/searchTable': "searchTable",
                'scripts/Utilities/signalr': "signalr",
                'scripts/Application/application': "application",
                'scripts/Map/cart': "cart",
                'scripts/Map/index': "map",
            }
        ]
    }]
