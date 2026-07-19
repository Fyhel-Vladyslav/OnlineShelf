// import React, { useState } from 'react';
// import { Form, Input, Button, Space, message, Select, ColorPicker } from 'antd';

// import { useNavigate } from 'react-router-dom';
// import { useShelfs } from '@/hooks/shelfs/useShelfs';
// import type { CreateItemDto } from '@/api/shelfs/itemsApi';
// import { useCreateItem } from '@/hooks/items/useCreateItem';
// import { useAttributeValues } from '@/hooks/items/useAttributeValues';
// import ImagePicker from '@/components/ImagePicker/ImagePicker';
// import ImageShow from '@/components/ImageShow/ImageShow';
// import { useNotification } from '@/notification/useNotification';
// import { isAxiosError } from 'axios';
// import { FavouriteButton } from '@/components/IsFavourite/IsFavourite';


// const { Option } = Select;

// const CreateItemPage: React.FC = () => {
//   const { notify } = useNotification();
//   const navigate = useNavigate();
//   const [form] = Form.useForm();
//   const isFavoriteFormValue = Form.useWatch('isFavorite', form);
//   const [loading, setLoading] = useState(false);
//   const { data: shelfs, isLoading: shelfsLoading } = useShelfs();
//   const { data: attributeValues, isLoading: attributesLoading } = useAttributeValues();
//   const [isDirty, setIsDirty] = useState(false);
//   const createItem = useCreateItem();
//   const isFavorite = isFavoriteFormValue || false;
//   // Get attribute options by type name
//   const getAttributeOptions = (typeName: string) => {
//     const attribute = attributeValues?.find(attr => attr.typeName === typeName);
//     return attribute?.options || [];
//   };



//   if (shelfsLoading || attributesLoading) {
//     return <div>Loading...</div>;
//   }
//   const handleToggle = () => {
    
//     form.setFieldValue('isFavorite', !isFavorite);
//   };
//   const handleSubmit = async (values: any) => {
//     setLoading(true);
//     try {
//       console.log('Creating item:', values);

//       const payload: CreateItemDto = {
//         name: values.name,
//         shelfId: values.shelfId,
//         bigImage: values.bigImage,
//         attributeColorMain: values.attributeColorMain,
//         attributeColorSecond: values.attributeColorSecond,
//         attributeType: values.attributeType,
//         attributeSeason: values.attributeSeason,
//         attributePattern: values.attributePattern,
//         attributeMatterial: values.attributeMatterial,
//         isFavorite: values.isFavorite,
        
//       };
//       console.log("ПЕРЕВІРКА ФАЙЛУ:", payload.bigImage, "Тип:", typeof payload.bigImage);
//       createItem.mutate(payload, {
//         onSuccess: () => {
//           message.success('Item created successfully');
//           navigate("/shelfs");
//         },
//         onError: (error) => {
//           message.error('Failed to create item');
//           let code: number | undefined;
//           if (isAxiosError(error)) {
//             code = error.response?.status;
//           }
//           notify({
//             code: code,
//             text: error.message,
//             time: 3000,
//         })
//         }
//       });
//     } catch (error) {
//       message.error('Failed to update item');
//     } finally {
//       setLoading(false);
//     }
//   };

//   return (
//     <div className="item-edit-page">
//       <h1>New Item</h1>
      
//       {/* 1. ПЕРЕНОСИМО <Form> НА САМИЙ ВЕРХ, щоб вона обгорнула обидві колонки */}
//       <Form
//         form={form}
//         layout="horizontal"
//         onFinish={handleSubmit}
//         // Прибираємо maxWidth: 800, бо тепер форма на всю ширину
//         labelCol={{ span: 6 }}
//         wrapperCol={{ span: 18 }}
//         onValuesChange={() => {
//           const current = form.getFieldsValue(true);
//           const hasName = current.name && current.name.trim().length > 0;
//           const hasShelfId = current.shelfId !== undefined && current.shelfId !== null;
//           setIsDirty(hasName && hasShelfId);
//         }}
//       >
//         <div style={{ display: 'flex', gap: '20px', height: "75vh" }}>
          
//           {/* ЛІВА КОЛОНКА (60%) */}
//           <div style={{ flex: '60%' }}>
//             <Form.Item
//               name="name"
//               label={<span style={{ fontSize: '16px', color: 'white' }}>Name</span>}
//               rules={[{ required: true, message: 'Please input the name!' }]}
//             >
//               <Input style={{ width: '100%' }} />
//             </Form.Item>

//             <Form.Item
//               name="shelfId"
//               label={<span style={{ fontSize: '16px', color: 'white' }}>Shelf</span>}
//               rules={[{ required: true, message: 'Please select a shelf!' }]}
//             >
//               <Select placeholder="Select a shelf" style={{ width: '100%' }}>
//                 {shelfs?.map(shelf => (
//                   <Option key={shelf.id} value={shelf.id}>
//                     {shelf.name}
//                   </Option>
//                 ))}
//               </Select>
//             </Form.Item>

//             {/* Color Pickers - Row 1 */}
//             <div style={{ display: 'flex', gap: '20px'}}>
//               <div style={{ flex: 1 }}>
//                 <Form.Item
//                   name="attributeColorMain"
//                   label={<span style={{ fontSize: '16px', color: 'white' }}>Color 1</span>}
//                   labelCol={{ span: 6 }}
//                   wrapperCol={{ span: 18 }}
//                   initialValue="#000000"
//                   getValueFromEvent={(color) => typeof color === 'string' ? color : color?.toHexString()}
//                 >
//                   <ColorPicker showText style={{width: '100px'}}/>
//                 </Form.Item>
//               </div>
//               <div style={{ flex: 1 }}>
//                 <Form.Item
//                   name="attributeColorSecond"
//                   label={<span style={{ fontSize: '16px', color: 'white' }}>Color 2</span>}
//                   labelCol={{ span: 6 }}
//                   wrapperCol={{ span: 18 }}
//                   initialValue="#000000"
//                   getValueFromEvent={(color) => typeof color === 'string' ? color : color?.toHexString()}
//                 >
//                   <ColorPicker showText style={{width: '100px'}}/>
//                 </Form.Item>
//               </div>
//             </div>

//             {/* Type and Season - Row 2 */}
//             <div style={{ display: 'flex', gap: '20px' }}>
//               <div style={{ flex: 1 }}>
//                 <Form.Item
//                   name="attributeType"
//                   label={<span style={{ fontSize: '16px', color: 'white' }}>Type</span>}
//                   labelCol={{ span: 6 }}
//                   wrapperCol={{ span: 18 }}
//                 >
//                   <Select placeholder="Select Type" style={{ width: '100%' }}>
//                     {getAttributeOptions('Type').map(option => (
//                       <Option key={option.key} value={option.key}>{option.value}</Option>
//                     ))}
//                   </Select>
//                 </Form.Item>
//               </div>
//               <div style={{ flex: 1 }}>
//                 <Form.Item
//                   name="attributeSeason"
//                   label={<span style={{ fontSize: '16px', color: 'white' }}>Season</span>}
//                   labelCol={{ span: 6 }}
//                   wrapperCol={{ span: 18 }}
//                 >
//                   <Select placeholder="Select Season" style={{ width: '100%' }}>
//                     {getAttributeOptions('Season').map(option => (
//                       <Option key={option.key} value={option.key}>{option.value}</Option>
//                     ))}
//                   </Select>
//                 </Form.Item>
//               </div>
//             </div>

//             {/* Pattern and Material - Row 3 */}
//             <div style={{ display: 'flex', gap: '20px' }}>
//               <div style={{ flex: 1 }}>
//                 <Form.Item
//                   name="attributePattern"
//                   label={<span style={{ fontSize: '16px', color: 'white' }}>Pattern</span>}
//                   labelCol={{ span: 6 }}
//                   wrapperCol={{ span: 18 }}
//                 >
//                   <Select placeholder="Select Pattern" style={{ width: '100%' }}>
//                     {getAttributeOptions('Pattern').map(option => (
//                       <Option key={option.key} value={option.key}>{option.value}</Option>
//                     ))}
//                   </Select>
//                 </Form.Item>
//               </div>
//               <div style={{ flex: 1 }}>
//                 <Form.Item
//                   name="attributeMatterial"
//                   label={<span style={{ fontSize: '16px', color: 'white' }}>Material</span>}
//                   labelCol={{ span: 6 }}
//                   wrapperCol={{ span: 18 }}
//                 >
//                   <Select placeholder="Select Material" style={{ width: '100%' }}>
//                     {getAttributeOptions('Material').map(option => (
//                       <Option key={option.key} value={option.key}>{option.value}</Option>
//                     ))}
//                   </Select>
//                 </Form.Item>
//               </div>
//             </div>

//             <Form.Item name="isFavorite" initialValue={false} hidden>
//               <Input />
//             </Form.Item>
            
//             <Form.Item wrapperCol={{ span: 24 }}>
//               <div style={{ textAlign: 'center' }}>
//                 <Space>
//                   <Button type="primary" htmlType="submit" loading={loading} disabled={!isDirty}
//                     style={!isDirty ? { backgroundColor: "#ffffff", color: "rgba(0, 0, 0, 0.25)", borderColor: "#d9d9d9" } : {}}>
//                     Save
//                   </Button>
//                   <Button onClick={() => navigate('/')}>Back</Button>
//                 </Space>
//               </div>
//             </Form.Item>
//           </div>

//           <div style={{ flex: '40%' }}>
//             <div style={{ position: 'relative', height: '100%', width: '100%', display: 'inline-block' }}>
//               <FavouriteButton 
//                 isFavorite={!!isFavorite}
//                 onToggle={handleToggle}
//                 style={{
//                   position: 'absolute' as const, top: '8px', right: '8px', zIndex: 10,
//                   backgroundColor: 'rgba(0,0,0,0.6)', borderRadius: '50%', padding: '4px'
//                 }}
//               />
//               <div style={{ height: '60%', marginBottom: '20px', padding: '10px', minHeight: '200px', border: '1px solid #d9d9d9', borderRadius: '4px', position: 'relative' }}>
//                 <ImageShow src={""} alt="Current Item" style={{ height: "100%"}} />
//               </div>
//               <div style={{display:"flex"}}>
//                 <div style={{width:"50%", padding:"3px"}}>
//                   <Button style={{width:"100%", height:"100%", fontSize:24}}>
//                     Detect using camera
//                   </Button>
//                 </div>
//                 <div style={{width:"50%", padding:"3px"}}>
                  
//                   {/* 2. ТУТ ВАЖЛИВО ДОДАТИ valuePropName */}
//                   <Form.Item name="bigImage" valuePropName="value">
//                     <ImagePicker />
//                   </Form.Item>
                  
//                 </div>
//               </div>
//             </div>
//           </div>

//         </div>
//       </Form>
//     </div>
//   );
// };

// export default CreateItemPage;

import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useCreateItem } from '@/hooks/items/useCreateItem';
import { ItemForm } from '@/features/item/ItemForm';
import { useNotification } from '@/notification/useNotification';
import { message } from 'antd';
import type { CreateItemDto } from '@/api/shelfs/itemsApi';

const CreateItemPage: React.FC = () => {
  const navigate = useNavigate();
  const { notify } = useNotification();
  const [loading, setLoading] = useState(false);
  
  const createItem = useCreateItem();

  // Дефолтні стартові значення для форми створення речі (як на фото)
  const defaultValues = {
    attributeColorMain: "#000000",
    attributeColorSecond: "#000000",
    attributeType: '-',
    attributeMatterial: '-',
    attributeSeason: '-',
    attributePattern: '-',
    isFavorite: false,
  };

  const handleSubmit = async (values: any) => {
    setLoading(true);
    try {
      const payload: CreateItemDto & { imageFile?: File } = {
        name: values.name,
        shelfId: values.shelfId,
        imageFile: values.bigImage instanceof File ? values.bigImage : undefined,
        attributeColorMain: values.attributeColorMain,
        attributeColorSecond: values.attributeColorSecond,
        attributeType: values.attributeType,
        attributeSeason: values.attributeSeason,
        attributePattern: values.attributePattern,
        attributeMatterial: values.attributeMatterial,
        isFavorite: values.isFavorite,
      };

      await createItem.mutateAsync(payload as any);
      message.success('Item created successfully');
      navigate("/shelfs");
    } catch (error: any) {
      message.error('Failed to create item');
      notify({ text: error.message, time: 3000 });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="item-create-page" style={{ backgroundColor: '#202020', minHeight: '100vh', padding: '40px', color: 'white', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
      <h1 style={{ color: 'white', fontSize: '50px', marginBottom: '50px' }}>New Item</h1>
      <ItemForm initialValues={defaultValues} loading={loading} onSubmit={handleSubmit} onBack={() => navigate('/shelfs')} />
    </div>
  );
};

export default CreateItemPage;