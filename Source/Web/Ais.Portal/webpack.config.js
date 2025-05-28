import path from "path";
import { fileURLToPath } from 'url';
const __dirname = path.dirname(fileURLToPath(import.meta.url));

const mode = process.env.NODE_ENV === 'development' ? 'development' : 'production';
const devtool = mode === 'production' ? 'source-map' : 'eval-source-map';
export default [
    {
        mode: mode,
        devtool: devtool,
        optimization: {
            minimize: devtool != 'production',
        },
        entry: {
            "core": './Scripts/Utilities/core.ts',
            "notification": './Scripts/Utilities/notification.ts',
            "resources": './Scripts/Utilities/resources.ts',
            "searchTable": './Scripts/Utilities/searchTable.ts',
            "signalr": './Scripts/Utilities/signalr.ts',
            "signing": './Scripts/Utilities/signing.ts',
            "applicationInfo": './Scripts/Application/applicationInfo.ts',
            "application": './Scripts/Application/application.ts',
            "folder": './Scripts/Folder/folder.ts',
            "indocuments": './Scripts/Documents/inDocuments.ts',
            "indocumentspal": './Scripts/Documents/inDocuments.ts',
            "account": './Scripts/Account/account.ts',
            "faq": './Scripts/Faq/faq.ts',
            "qualifiedPersons_application": './Areas/QualifiedPersons/Scripts/application.ts',
            "cart": './Scripts/Map/cart.ts',
            "map": './Scripts/Map/index.ts',
            "login": './Scripts/Account/login.ts',
            "clients": './Scripts/Clients/Clients.ts',
            "home": './Scripts/Home/Home.ts',
            "oszStaff": './Scripts/OSZStaff/oszStaff.ts',
            "qualifiedpersonsregister": './Scripts/QualifiedPersonsRegister/QualifiedPersonsRegister.ts',
            "outAdministrativeAct_admactregister": './Areas/OutAdministrativeAct/Scripts/AdmActRegister.ts',
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
                'scripts/Utilities/signing': "signing",
                'scripts/Application/application': "application",
                'scripts/Map/cart': "cart",
                'scripts/Map/map': "map",
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
